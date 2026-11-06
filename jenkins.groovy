def PROJECT_NAME = 'Butterfly_Match'
def UNITY_VERSION = '6000.3.19f1'
def UNITY_INSTALLATION = "/var/lib/jenkins/Unity/Hub/Editor/${UNITY_VERSION}/Editor"

pipeline {
    agent any

    triggers {
        // This tells Jenkins to check GitHub for changes every 5 minutes.
        pollSCM('H/5 * * * *')
    }

    options {
        timestamps()
        disableConcurrentBuilds()
        skipDefaultCheckout(true)
    }

    parameters {
        booleanParam(name: 'BUILD_ANDROID_APK', defaultValue: false, description: 'Build Android APK')
        booleanParam(name: 'DEPLOY_ANDROID_APK', defaultValue: false, description: 'Upload APK to Nexus')
        booleanParam(name: 'BUILD_ANDROID_AAB', defaultValue: false, description: 'Build Android AAB')
        booleanParam(name: 'DEPLOY_ANDROID_AAB', defaultValue: false, description: 'Upload AAB to Nexus')
    }

    environment {
        UNITY_PATH = "${UNITY_INSTALLATION}"

        NEXUS_IP_ADDRESS = 'http://127.0.0.1:8081'
        NEXUS_USERNAME = 'admin'
        NEXUS_PASSWORD = credentials('NEXUS_PASSWORD')

        REPO_DIR = "${WORKSPACE}/src"
    }

    stages {
        stage('Checkout & Setup Workspace') {
            steps {
                sh '''
                    set -e
                    mkdir -p "$REPO_DIR"
                    mkdir -p "$REPO_DIR/JenkinsLogs"
                    mkdir -p "$REPO_DIR/Signing"
                    mkdir -p "$REPO_DIR/Builds/AndroidAPK"
                    mkdir -p "$REPO_DIR/Builds/AndroidAAB"
                '''

                dir('src') {
                    script {
                        def scmVars = checkout([
                            $class: 'GitSCM',
                            branches: scm.branches,
                            doGenerateSubmoduleConfigurations: scm.doGenerateSubmoduleConfigurations,
                            // Added 'CleanBeforeCheckout' so Git cleans safely without destroying the LFS cache
                            extensions: (scm.extensions ?: []) + [[$class: 'GitLFSPull'], [$class: 'CleanBeforeCheckout']],
                            userRemoteConfigs: scm.userRemoteConfigs
                        ])
                        env.BUILD_BRANCH = scmVars.GIT_BRANCH ?: 'unknown'
                    }
                    sh '''
                        set -e
                        mkdir -p JenkinsLogs Signing Builds/AndroidAPK Builds/AndroidAAB
                        git rev-parse --short HEAD
                    '''
                }
            }
        }

        stage('Build Android APK') {
            when {
                expression {
                    params.BUILD_ANDROID_APK || env.BUILD_BRANCH.contains('develop')
                }
            }
            steps {
                withCredentials([
                    file(credentialsId: 'ITIC_GAMES.keystore', variable: 'JENKINS_KEYSTORE_FILE'),
                    string(credentialsId: 'ITIC_GAMES_KEYSTORE_PASS', variable: 'JENKINS_KEYSTORE_PASS'),
                    string(credentialsId: 'ITIC_GAMES_ALIAS', variable: 'JENKINS_ALIAS_NAME'),
                    string(credentialsId: 'ITIC_GAMES_ALIAS_PAS', variable: 'JENKINS_ALIAS_PASS')
                ]) {
                    sh '''
                        set -e

                        # =========================================================================
                        # WORKAROUND: Unity 2022.3 has a SIGFPE divide-by-zero bug on ZFS/Docker
                        # filesystems. We build in a /tmp sandbox to bypass this native crash.
                        # =========================================================================

                        TMP_BUILD_DIR="/tmp/butterfly_match_build_${BUILD_NUMBER}_APK"
                        # Remove the sandbox on any exit, including a failed Unity build
                        trap 'rm -rf "$TMP_BUILD_DIR"' EXIT
                        rm -rf "$TMP_BUILD_DIR"
                        mkdir -p "$TMP_BUILD_DIR"

                        echo "Copying project to safe /tmp partition..."
                        rsync -a "$REPO_DIR/" "$TMP_BUILD_DIR/"

                        mkdir -p "$TMP_BUILD_DIR/Signing"

                        # Setup Keystore
                        cp "$JENKINS_KEYSTORE_FILE" "$TMP_BUILD_DIR/Signing/android.keystore"
                        chmod 600 "$TMP_BUILD_DIR/Signing/android.keystore"

                        export ITIC_GAMES_KEYSTORE_FILE="$TMP_BUILD_DIR/Signing/android.keystore"
                        export ITIC_GAMES_KEYSTORE_PASS="$JENKINS_KEYSTORE_PASS"
                        export ITIC_GAMES_ALIAS="$JENKINS_ALIAS_NAME"
                        export ITIC_GAMES_ALIAS_PAS="$JENKINS_ALIAS_PASS"
                        export Butterfly_Match_BUILD_NUMBER="$BUILD_NUMBER"
                        export GRADLE_USER_HOME=/var/lib/jenkins/.gradle

                        export UNITY_BURST_DISABLE_COMPILATION=1

                        # Amputate toxic demo folders
                        rm -rf "$TMP_BUILD_DIR/Assets/Plugins/Feel/NiceVibrations/Demo"
                        rm -rf "$TMP_BUILD_DIR/Assets/Plugins/Feel/MMTools/Accessories/MMShaders"
                        rm -rf "$TMP_BUILD_DIR/Assets/Plugins/PlaceHolderVFX/JMO Assets/Cartoon FX Remaster/Demo Assets"
                        rm -rf "$TMP_BUILD_DIR/Assets/Plugins/Sirenix/Odin Inspector/Modules/Unity.Mathematics"

                        mkdir -p "$REPO_DIR/JenkinsLogs" "$REPO_DIR/Builds/AndroidAPK" "$REPO_DIR/Signing" "$TMP_BUILD_DIR/JenkinsLogs"

                        echo "Starting Unity build..."
                        set +e
                        xvfb-run --auto-servernum --server-args="-screen 0 1920x1080x24" \
                        "$UNITY_PATH/Unity" \
                          -quit -batchmode -nographics \
                          -buildTarget Android \
                          -projectPath "$TMP_BUILD_DIR" \
                          -executeMethod Editor.BuildScript.BuildAndroid \
                          -job-worker-count 2 \
                          -buildType APK \
                          -logFile "$TMP_BUILD_DIR/JenkinsLogs/unity_android_apk.log"
                        UNITY_EXIT=$?
                        set -e

                        mkdir -p "$REPO_DIR/JenkinsLogs"
                        cp "$TMP_BUILD_DIR/JenkinsLogs/unity_android_apk.log" "$REPO_DIR/JenkinsLogs/unity_android_apk.log" || true
                        cp "$TMP_BUILD_DIR/Library/LastBuild.buildreport" "$REPO_DIR/JenkinsLogs/LastBuild_APK.buildreport" || true

                        if [ "$UNITY_EXIT" -ne 0 ]; then
                          exit "$UNITY_EXIT"
                        fi

                        echo "Retrieving APK..."
                        mkdir -p "$REPO_DIR/Builds/AndroidAPK"
                        cp -r "$TMP_BUILD_DIR/Builds/AndroidAPK/"* "$REPO_DIR/Builds/AndroidAPK/" || true
                    '''
                }

                sh 'ls -la "$REPO_DIR/Builds/AndroidAPK" || true'
            }
        }

        stage('Deploy Android APK to Nexus') {
            when {
                expression {
                    params.DEPLOY_ANDROID_APK || env.BUILD_BRANCH.contains('develop')
                }
            }
            steps {
                script {
                    def buildDate = new Date().format('yyyyMMdd_HHmm')
                    env.ARTIFACT_NAME = "Android_${PROJECT_NAME}_${buildDate}.apk"
                }
                sh '''
                    set -e

                    APK_PATH="$(ls -1 "$REPO_DIR/Builds/AndroidAPK/"*.apk 2>/dev/null | head -n 1)"
                    test -f "$APK_PATH"

                    curl -sS -u "$NEXUS_USERNAME:$NEXUS_PASSWORD" \
                      --upload-file "$APK_PATH" \
                      "$NEXUS_IP_ADDRESS/repository/ITIC_GAMES/AndroidAPK_Builds/$ARTIFACT_NAME"
                '''
            }
        }

        stage('Build Android AAB') {
            when {
                expression {
                    params.BUILD_ANDROID_AAB || env.BUILD_BRANCH.contains('main')
                }
            }
            steps {
                withCredentials([
                    file(credentialsId: 'ITIC_GAMES.keystore', variable: 'JENKINS_KEYSTORE_FILE'),
                    string(credentialsId: 'ITIC_GAMES_KEYSTORE_PASS', variable: 'JENKINS_KEYSTORE_PASS'),
                    string(credentialsId: 'ITIC_GAMES_ALIAS', variable: 'JENKINS_ALIAS_NAME'),
                    string(credentialsId: 'ITIC_GAMES_ALIAS_PAS', variable: 'JENKINS_ALIAS_PASS')
                ]) {
                    sh '''
                        set -e

                        TMP_BUILD_DIR="/tmp/butterfly_match_build_${BUILD_NUMBER}_AAB"
                        # Remove the sandbox on any exit, including a failed Unity build
                        trap 'rm -rf "$TMP_BUILD_DIR"' EXIT
                        rm -rf "$TMP_BUILD_DIR"
                        mkdir -p "$TMP_BUILD_DIR"

                        echo "Copying project to safe /tmp partition..."
                        rsync -a "$REPO_DIR/" "$TMP_BUILD_DIR/"

                        mkdir -p "$TMP_BUILD_DIR/Signing"

                        # Setup Keystore
                        cp "$JENKINS_KEYSTORE_FILE" "$TMP_BUILD_DIR/Signing/android.keystore"
                        chmod 600 "$TMP_BUILD_DIR/Signing/android.keystore"

                        export ITIC_GAMES_KEYSTORE_FILE="$TMP_BUILD_DIR/Signing/android.keystore"
                        export ITIC_GAMES_KEYSTORE_PASS="$JENKINS_KEYSTORE_PASS"
                        export ITIC_GAMES_ALIAS="$JENKINS_ALIAS_NAME"
                        export ITIC_GAMES_ALIAS_PAS="$JENKINS_ALIAS_PASS"
                        export Butterfly_Match_BUILD_NUMBER="$BUILD_NUMBER"
                        export GRADLE_USER_HOME=/var/lib/jenkins/.gradle

                        export UNITY_BURST_DISABLE_COMPILATION=1

                        # Amputate toxic demo folders
                        rm -rf "$TMP_BUILD_DIR/Assets/Plugins/Feel/NiceVibrations/Demo"
                        rm -rf "$TMP_BUILD_DIR/Assets/Plugins/Feel/MMTools/Accessories/MMShaders"
                        rm -rf "$TMP_BUILD_DIR/Assets/Plugins/PlaceHolderVFX/JMO Assets/Cartoon FX Remaster/Demo Assets"
                        rm -rf "$TMP_BUILD_DIR/Assets/Plugins/Sirenix/Odin Inspector/Modules/Unity.Mathematics"

                        mkdir -p "$REPO_DIR/JenkinsLogs" "$REPO_DIR/Builds/AndroidAAB" "$REPO_DIR/Signing" "$TMP_BUILD_DIR/JenkinsLogs"

                        echo "Starting Unity build..."
                        set +e
                        xvfb-run --auto-servernum --server-args="-screen 0 1920x1080x24" \
                        "$UNITY_PATH/Unity" \
                          -quit -batchmode -nographics \
                          -buildTarget Android \
                          -projectPath "$TMP_BUILD_DIR" \
                          -executeMethod Editor.BuildScript.BuildAndroid \
                          -job-worker-count 2 \
                          -buildType AAB \
                          -logFile "$TMP_BUILD_DIR/JenkinsLogs/unity_android_aab.log"
                        UNITY_EXIT=$?
                        set -e

                        mkdir -p "$REPO_DIR/JenkinsLogs"
                        cp "$TMP_BUILD_DIR/JenkinsLogs/unity_android_aab.log" "$REPO_DIR/JenkinsLogs/unity_android_aab.log" || true
                        cp "$TMP_BUILD_DIR/Library/LastBuild.buildreport" "$REPO_DIR/JenkinsLogs/LastBuild_AAB.buildreport" || true

                        if [ "$UNITY_EXIT" -ne 0 ]; then
                          exit "$UNITY_EXIT"
                        fi

                        echo "Retrieving AAB..."
                        mkdir -p "$REPO_DIR/Builds/AndroidAAB"
                        cp -r "$TMP_BUILD_DIR/Builds/AndroidAAB/"* "$REPO_DIR/Builds/AndroidAAB/" || true
                    '''
                }

                sh 'ls -la "$REPO_DIR/Builds/AndroidAAB" || true'
            }
        }

        stage('Deploy Android AAB to Nexus') {
            when {
                expression {
                    params.DEPLOY_ANDROID_AAB || env.BUILD_BRANCH.contains('main')
                }
            }
            steps {
                script {
                    def buildDate = new Date().format('yyyyMMdd_HHmm')
                    env.ARTIFACT_NAME = "Android_${PROJECT_NAME}_${buildDate}.aab"
                }
                sh '''
                    set -e

                    AAB_PATH="$(ls -1 "$REPO_DIR/Builds/AndroidAAB/"*.aab 2>/dev/null | head -n 1)"
                    test -f "$AAB_PATH"

                    curl -sS -u "$NEXUS_USERNAME:$NEXUS_PASSWORD" \
                      --upload-file "$AAB_PATH" \
                      "$NEXUS_IP_ADDRESS/repository/ITIC/AndroidAAB_Builds/$ARTIFACT_NAME"
                '''
            }
        }
    }

    post {
        always {
            archiveArtifacts artifacts: 'src/JenkinsLogs/*.log', allowEmptyArchive: true
            archiveArtifacts artifacts: 'src/JenkinsLogs/*.buildreport', allowEmptyArchive: true
            archiveArtifacts artifacts: 'src/Builds/**', allowEmptyArchive: true
        }
        success {
            withCredentials([string(credentialsId: 'TELEGRAM_BOT_TOKEN', variable: 'BOT_TOKEN')]) {
                script {
                    def TELEGRAM_CHAT_ID = '-1002435889483'
                    def TELEGRAM_THREAD_ID = '1236'
                    def completedAt = new Date().format('yyyy-MM-dd HH:mm:ss z', TimeZone.getTimeZone('Asia/Tashkent'))
                    def commitShort = sh(script: 'git -C src rev-parse --short HEAD', returnStdout: true).trim()
                    def buildUrl = env.BUILD_URL ?: ''
                    def htmlEscape = { value ->
                        return (value ?: '')
                            .replace('&', '&amp;')
                            .replace('<', '&lt;')
                            .replace('>', '&gt;')
                    }

                    def writeSuccessCaption = { artifactPath, artifactType ->
                        def artifactName = sh(script: "basename '${artifactPath}'", returnStdout: true).trim()
                        def artifactSize = sh(script: "du -h '${artifactPath}' | awk '{print \$1}'", returnStdout: true).trim()
                        def caption =
                            "<b>Butterfly Match Android ${htmlEscape(artifactType)} build succeeded</b>\n" +
                            "<pre>" +
                            "Project: ${htmlEscape(PROJECT_NAME)}\n" +
                            "Job: ${htmlEscape(env.JOB_NAME ?: 'Butterfly Match')}\n" +
                            "Build: #${htmlEscape(env.BUILD_NUMBER)}\n" +
                            "Branch: ${htmlEscape(env.BUILD_BRANCH ?: 'unknown')}\n" +
                            "Commit: ${htmlEscape(commitShort)}\n" +
                            "Unity: ${htmlEscape(UNITY_VERSION)}\n" +
                            "Artifact: ${htmlEscape(artifactName)}\n" +
                            "Size: ${htmlEscape(artifactSize)}\n" +
                            "Completed: ${htmlEscape(completedAt)}" +
                            "</pre>"

                        if (buildUrl) {
                            caption += "\n<a href=\"${htmlEscape(buildUrl)}\">Open Jenkins build</a>"
                        }

                        writeFile file: 'tg_success_caption.html', text: caption
                    }

                    // 1. Upload APK Directly into Telegram Chat
                    if (params.BUILD_ANDROID_APK || (env.BUILD_BRANCH != null && env.BUILD_BRANCH.contains('develop'))) {
                        def APK_PATH = sh(script: 'ls -1 src/Builds/AndroidAPK/*.apk 2>/dev/null | head -n 1', returnStdout: true).trim()
                        if (APK_PATH) {
                            writeSuccessCaption(APK_PATH, 'APK')
                            withEnv(["TELEGRAM_CHAT_ID=${TELEGRAM_CHAT_ID}", "TELEGRAM_THREAD_ID=${TELEGRAM_THREAD_ID}", "ARTIFACT_PATH=${APK_PATH}"]) {
                                sh '''
                                    curl -s -X POST "http://127.0.0.1:8082/bot${BOT_TOKEN}/sendDocument" \
                                      -F chat_id="${TELEGRAM_CHAT_ID}" \
                                      -F message_thread_id="${TELEGRAM_THREAD_ID}" \
                                      -F document=@"${ARTIFACT_PATH}" \
                                      -F caption="<tg_success_caption.html" \
                                      -F parse_mode="HTML"
                                '''
                            }
                        }
                    }

                    // 2. Upload AAB Directly into Telegram Chat
                    if (params.BUILD_ANDROID_AAB || (env.BUILD_BRANCH != null && env.BUILD_BRANCH.contains('main'))) {
                        def AAB_PATH = sh(script: 'ls -1 src/Builds/AndroidAAB/*.aab 2>/dev/null | head -n 1', returnStdout: true).trim()
                        if (AAB_PATH) {
                            writeSuccessCaption(AAB_PATH, 'AAB')
                            withEnv(["TELEGRAM_CHAT_ID=${TELEGRAM_CHAT_ID}", "TELEGRAM_THREAD_ID=${TELEGRAM_THREAD_ID}", "ARTIFACT_PATH=${AAB_PATH}"]) {
                                sh '''
                                    curl -s -X POST "http://127.0.0.1:8082/bot${BOT_TOKEN}/sendDocument" \
                                      -F chat_id="${TELEGRAM_CHAT_ID}" \
                                      -F message_thread_id="${TELEGRAM_THREAD_ID}" \
                                      -F document=@"${ARTIFACT_PATH}" \
                                      -F caption="<tg_success_caption.html" \
                                      -F parse_mode="HTML"
                                '''
                            }
                        }
                    }
                }
            }
        }
        failure {
            sh 'tail -n 200 src/JenkinsLogs/unity_android_apk.log 2>/dev/null || true'
            sh 'tail -n 200 src/JenkinsLogs/unity_android_aab.log 2>/dev/null || true'

            withCredentials([string(credentialsId: 'TELEGRAM_BOT_TOKEN', variable: 'BOT_TOKEN')]) {
                script {
                    def TELEGRAM_CHAT_ID = '-1002435889483'
                    def TELEGRAM_THREAD_ID = '1236'

                    def MESSAGE = 'Build Failed!\n' +
                                  "Project: ${PROJECT_NAME}\n" +
                                  "Branch: ${env.BUILD_BRANCH ?: 'unknown'}\n" +
                                  'Check Jenkins logs for details.'

                    writeFile file: 'tg_fail_message.txt', text: MESSAGE

                    // Send the failure message to your local server!
                    sh """
                        curl -s -X POST http://127.0.0.1:8082/bot${BOT_TOKEN}/sendMessage \
                        -d chat_id="${TELEGRAM_CHAT_ID}" \
                        -d message_thread_id="${TELEGRAM_THREAD_ID}" \
                        --data-urlencode text@tg_fail_message.txt
                    """
                }
            }
        }
    }
}
