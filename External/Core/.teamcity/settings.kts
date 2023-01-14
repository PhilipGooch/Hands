import jetbrains.buildServer.configs.kotlin.v2019_2.*
import jetbrains.buildServer.configs.kotlin.v2019_2.buildFeatures.Swabra
import jetbrains.buildServer.configs.kotlin.v2019_2.buildFeatures.XmlReport
import jetbrains.buildServer.configs.kotlin.v2019_2.buildFeatures.commitStatusPublisher
import jetbrains.buildServer.configs.kotlin.v2019_2.buildFeatures.swabra
import jetbrains.buildServer.configs.kotlin.v2019_2.buildFeatures.xmlReport
import jetbrains.buildServer.configs.kotlin.v2019_2.buildSteps.dotnetRun
import jetbrains.buildServer.configs.kotlin.v2019_2.buildSteps.exec
import jetbrains.buildServer.configs.kotlin.v2019_2.buildSteps.script
import jetbrains.buildServer.configs.kotlin.v2019_2.failureConditions.BuildFailureOnText
import jetbrains.buildServer.configs.kotlin.v2019_2.failureConditions.failOnText
import jetbrains.buildServer.configs.kotlin.v2019_2.triggers.retryBuild
import jetbrains.buildServer.configs.kotlin.v2019_2.triggers.schedule
import jetbrains.buildServer.configs.kotlin.v2019_2.vcs.GitVcsRoot

/*
The settings script is an entry point for defining a TeamCity
project hierarchy. The script should contain a single call to the
project() function with a Project instance or an init function as
an argument.

VcsRoots, BuildTypes, Templates, and subprojects can be
registered inside the project using the vcsRoot(), buildType(),
template(), and subProject() methods respectively.

To debug settings scripts in command-line, run the

    mvnDebug org.jetbrains.teamcity:teamcity-configs-maven-plugin:generate

command and attach your debugger to the port 8000.

To debug in IntelliJ Idea, open the 'Maven Projects' tool window (View
-> Tool Windows -> Maven Projects), find the generate task node
(Plugins -> teamcity-configs -> teamcity-configs:generate), the
'Debug' option is available in the context menu for the task.
*/

version = "2022.04"

project {

    vcsRoot(Core)

    buildType(RunRuntimeTestsSwitchDevelopment)
    buildType(BuildSwitchDevelopment)
    buildType(BuildWindowsIl2cppDevelopment)
    buildType(BuildWindowsMonoRelease)
    buildType(RunEditorTestsEditPlayModes)
    buildType(BuildWindowsDevelopment)
    buildType(DocsGenerate)
    buildType(RunRuntimeTests)
    buildType(BuildWindowsIl2cppRelease)
    buildType(BuildSwitchRelease)
    buildType(RunRuntimeTestsSwitchRelease)
    buildType(VerifyEverything)
    buildType(RunRuntimeTestsWindowsIl2cppRelease)
    buildType(VerifyBuild)

    template(RunEditorTestsRootTemplate)
    template(RunRuntimeTests_2)
    template(BuildCoreTemplate)

    params {
        text("automation.Config", "core.automation.json", display = ParameterDisplay.HIDDEN, readOnly = true, allowEmpty = false)
        text("automation.WorkDir", "Tools/Automation", display = ParameterDisplay.HIDDEN, readOnly = true, allowEmpty = false)
    }
    buildTypesOrder = arrayListOf(VerifyBuild, BuildSwitchDevelopment, BuildSwitchRelease, BuildWindowsIl2cppDevelopment, BuildWindowsIl2cppRelease, BuildWindowsDevelopment, BuildWindowsMonoRelease, RunEditorTestsEditPlayModes, RunRuntimeTestsSwitchDevelopment, RunRuntimeTestsSwitchRelease, RunRuntimeTests, RunRuntimeTestsWindowsIl2cppRelease, DocsGenerate)

    subProject(Utilities)
}

object BuildSwitchDevelopment : BuildType({
    templates(BuildCoreTemplate)
    name = "Build - Switch (Development)"

    params {
        param("automation.Configuration", "Development")
        param("automation.Platform", "Switch")
    }

    failureConditions {
        executionTimeoutMin = 30
    }

    requirements {
        exists("NBG_CAN_BUILD_SWITCH", "RQ_18")
    }
})

object BuildSwitchRelease : BuildType({
    templates(BuildCoreTemplate)
    name = "Build - Switch (Release)"

    params {
        param("automation.Configuration", "Release")
        param("automation.Platform", "Switch")
    }

    failureConditions {
        executionTimeoutMin = 30
    }

    requirements {
        exists("NBG_CAN_BUILD_SWITCH", "RQ_18")
    }
})

object BuildWindowsDevelopment : BuildType({
    templates(BuildCoreTemplate)
    name = "Build - Windows Mono (Development)"

    params {
        param("automation.ScriptingBackend", "Mono")
        param("automation.Configuration", "Development")
        param("automation.Platform", "Windows")
    }
})

object BuildWindowsIl2cppDevelopment : BuildType({
    templates(BuildCoreTemplate)
    name = "Build - Windows IL2CPP (Development)"

    params {
        param("automation.ScriptingBackend", "IL2CPP")
        param("automation.Configuration", "Development")
        param("automation.Platform", "Windows")
    }

    failureConditions {
        executionTimeoutMin = 30
    }
})

object BuildWindowsIl2cppRelease : BuildType({
    templates(BuildCoreTemplate)
    name = "Build - Windows IL2CPP (Release)"

    params {
        param("automation.Configuration", "Release")
        param("automation.Platform", "Windows")
    }

    failureConditions {
        executionTimeoutMin = 30
    }
})

object BuildWindowsMonoRelease : BuildType({
    templates(BuildCoreTemplate)
    name = "Build - Windows Mono (Release)"

    params {
        param("automation.ScriptingBackend", "Mono")
        param("automation.Configuration", "Release")
        param("automation.Platform", "Windows")
    }
})

object DocsGenerate : BuildType({
    name = "Docs - Generate"

    artifactRules = "BuildSystem/Artifacts/Documentation/** => docs.7z"

    vcs {
        root(Core)
    }

    steps {
        exec {
            name = "Cleanup"
            workingDir = "Documentation"
            path = "clean.bat"
            param("script.content", "build.bat")
        }
        exec {
            name = "Generate Projects"
            workingDir = "Documentation"
            path = "generateProjects.bat"
            param("script.content", "build.bat")
        }
        exec {
            name = "Build"
            workingDir = "Documentation"
            path = "build.bat"
            arguments = "--force"
            param("script.content", "build.bat")
        }
    }

    failureConditions {
        failOnText {
            conditionType = BuildFailureOnText.ConditionType.CONTAINS
            pattern = "Msbuild failed"
            reverse = false
        }
    }

    requirements {
        exists("DotNetCLI_Path")
        exists("NBG_HAS_UNITY")
    }
})

object RunEditorTestsEditPlayModes : BuildType({
    templates(RunEditorTestsRootTemplate)
    name = "Run - Editor Tests (Edit & Play Modes)"

    vcs {
        root(Core)
    }
    
    disableSettings("RUNNER_14")
})

object RunRuntimeTests : BuildType({
    templates(RunRuntimeTests_2)
    name = "Run - Runtime Tests - Windows IL2CPP (Development)"

    params {
        param("automation.Platform", "Windows")
        param("automation.Target", "")
    }

    dependencies {
        dependency(BuildWindowsIl2cppDevelopment) {
            snapshot {
            }

            artifacts {
                id = "ARTIFACT_DEPENDENCY_5"
                cleanDestination = true
                artifactRules = """Build/CoreSample-*.zip!** => BuildSystem\Dependencies\Build"""
            }
        }
    }
})

object RunRuntimeTestsSwitchDevelopment : BuildType({
    templates(RunRuntimeTests_2)
    name = "Run - Runtime Tests - Switch (Development)"

    params {
        param("automation.Platform", "Switch")
        param("automation.Target", "%NBG_SWITCH_ADDRESS%")
    }

    dependencies {
        dependency(BuildSwitchDevelopment) {
            snapshot {
            }

            artifacts {
                id = "ARTIFACT_DEPENDENCY_5"
                cleanDestination = true
                artifactRules = """Build/CoreSample-*.zip!** => BuildSystem\Dependencies\Build"""
            }
        }
    }
})

object RunRuntimeTestsSwitchRelease : BuildType({
    templates(RunRuntimeTests_2)
    name = "Run - Runtime Tests - Switch (Release)"

    params {
        param("automation.Platform", "Switch")
        param("automation.Target", "%NBG_SWITCH_ADDRESS%")
    }

    dependencies {
        dependency(BuildSwitchRelease) {
            snapshot {
            }

            artifacts {
                id = "ARTIFACT_DEPENDENCY_5"
                cleanDestination = true
                artifactRules = """Build/CoreSample-*.zip!** => BuildSystem\Dependencies\Build"""
            }
        }
    }
})

object RunRuntimeTestsWindowsIl2cppRelease : BuildType({
    templates(RunRuntimeTests_2)
    name = "Run - Runtime Tests - Windows IL2CPP (Release)"

    params {
        param("automation.Platform", "Windows")
        param("automation.Target", "")
    }

    dependencies {
        dependency(BuildWindowsIl2cppRelease) {
            snapshot {
            }

            artifacts {
                id = "ARTIFACT_DEPENDENCY_5"
                cleanDestination = true
                artifactRules = """Build/CoreSample-*.zip!** => BuildSystem\Dependencies\Build"""
            }
        }
    }
})

object VerifyBuild : BuildType({
    name = "Verify Build"

    allowExternalStatus = true
    type = BuildTypeSettings.Type.COMPOSITE

    vcs {
        root(Core)

        excludeDefaultBranchChanges = true
    }

    features {
        commitStatusPublisher {
            vcsRootExtId = "${Core.id}"
            publisher = github {
                githubUrl = "https://api.github.com"
                authType = personalToken {
                    token = "credentialsJSON:b5c295c6-72f1-4959-a6a7-9456a1d038f7"
                }
            }
        }
    }

    dependencies {
        snapshot(BuildWindowsDevelopment) {
        }
        snapshot(RunEditorTestsEditPlayModes) {
        }
        snapshot(RunRuntimeTests) {
        }
        snapshot(RunRuntimeTestsWindowsIl2cppRelease) {
        }
    }
})

object VerifyEverything : BuildType({
    name = "Verify Everything"

    type = BuildTypeSettings.Type.COMPOSITE

    vcs {
        showDependenciesChanges = true
    }

    dependencies {
        snapshot(BuildWindowsMonoRelease) {
        }
        snapshot(DocsGenerate) {
        }
        snapshot(RunRuntimeTestsSwitchDevelopment) {
        }
        snapshot(RunRuntimeTestsSwitchRelease) {
        }
        snapshot(VerifyBuild) {
        }
    }
})

object BuildCoreTemplate : Template({
    name = "Build Core (Template)"

    params {
        param("automation.ScriptingBackend", "IL2CPP")
        param("automation.Configuration", "")
        param("automation.Platform", "")
    }

    vcs {
        root(Core)

        checkoutMode = CheckoutMode.ON_AGENT
    }

    steps {
        dotnetRun {
            name = "Initialize Unity project"
            id = "RUNNER_21"
            workingDir = "%automation.WorkDir%"
            args = "--config %automation.Config% initializeUnityProject"
            param("dotNetCoverage.dotCover.home.path", "%teamcity.tool.JetBrains.dotCover.CommandLineTools.DEFAULT%")
        }
        dotnetRun {
            name = "Write Version.json"
            id = "RUNNER_16"
            workingDir = "%automation.WorkDir%"
            args = "--config %automation.Config% writeBuildVersion %build.number%"
            param("dotNetCoverage.dotCover.home.path", "%teamcity.tool.JetBrains.dotCover.CommandLineTools.DEFAULT%")
        }
        dotnetRun {
            name = "Build with Unity"
            id = "RUNNER_17"
            workingDir = "%automation.WorkDir%"
            args = """--config %automation.Config% buildUnityProject %automation.Platform% %automation.Configuration% --scripting=%automation.ScriptingBackend% --buildVersion="%build.number%""""
            param("dotNetCoverage.dotCover.home.path", "%teamcity.tool.JetBrains.dotCover.CommandLineTools.DEFAULT%")
        }
    }

    features {
        swabra {
            id = "swabra"
            filesCleanup = Swabra.FilesCleanup.DISABLED
            lockingProcesses = Swabra.LockingProcessPolicy.KILL
        }
    }

    requirements {
        exists("NBG_CAN_BUILD", "RQ_13")
    }
})

object RunEditorTestsRootTemplate : Template({
    name = "Run - Editor Tests (Root Template)"

    params {
        param("automation.TestPlatform", "All")
    }

    vcs {
        checkoutMode = CheckoutMode.ON_AGENT
    }

    steps {
        dotnetRun {
            name = "Run editor tests"
            id = "RUNNER_10"
            workingDir = "%automation.WorkDir%"
            args = "--config %automation.Config% --requireInteractiveShell runUnityTests %automation.TestPlatform%"
            param("dotNetCoverage.dotCover.home.path", "%teamcity.tool.JetBrains.dotCover.CommandLineTools.DEFAULT%")
        }
    }

    triggers {
        schedule {
            id = "TRIGGER_7"
            enabled = false
            schedulingPolicy = daily {
                hour = 1
            }
            branchFilter = """
                +:*
                -:teamcity
            """.trimIndent()
            triggerBuild = always()
        }
        retryBuild {
            id = "retryBuildTrigger"
            enabled = false
            attempts = 1
            moveToTheQueueTop = true
        }
    }

    features {
        xmlReport {
            id = "BUILD_EXT_1"
            reportType = XmlReport.XmlReportType.NUNIT
            rules = "+:BuildSystem/Artifacts/unity.tests.*.xml"
            verbose = true
        }
        swabra {
            id = "swabra"
            filesCleanup = Swabra.FilesCleanup.DISABLED
            lockingProcesses = Swabra.LockingProcessPolicy.KILL
        }
    }

    requirements {
        exists("NBG_CAN_BUILD", "RQ_4")
    }
})

object RunRuntimeTests_2 : Template({
    name = "Run - Runtime Tests"

    vcs {
        root(Core)

        checkoutMode = CheckoutMode.ON_AGENT
    }

    steps {
        dotnetRun {
            name = "Run runtime tests"
            id = "RUNNER_26"
            workingDir = "%automation.WorkDir%"
            args = "--config %automation.Config% runRuntimeTests %automation.Platform% %automation.Target%"
            param("dotNetCoverage.dotCover.home.path", "%teamcity.tool.JetBrains.dotCover.CommandLineTools.DEFAULT%")
        }
    }

    features {
        swabra {
            id = "swabra"
            filesCleanup = Swabra.FilesCleanup.DISABLED
            lockingProcesses = Swabra.LockingProcessPolicy.KILL
        }
    }

    requirements {
        exists("NBG_CAN_RUN_RUNTIME_TESTS", "RQ_17")
    }
})

object Core : GitVcsRoot({
    name = "Core"
    url = "git@github.com:NoBrakesGames/Core.git"
    branch = "refs/heads/develop"
    branchSpec = """
        +:refs/heads/(develop)
        +:refs/heads/*
    """.trimIndent()
    agentCleanPolicy = GitVcsRoot.AgentCleanPolicy.ALWAYS
    agentCleanFilesPolicy = GitVcsRoot.AgentCleanFilesPolicy.NON_IGNORED_ONLY
    checkoutPolicy = GitVcsRoot.AgentCheckoutPolicy.USE_MIRRORS
    authMethod = uploadedKey {
        uploadedKey = "GitHub (bot@)"
    }
})


object Utilities : Project({
    name = "Utilities"

    buildType(Utilities_UtilityInstallUnityEditorOnBuildAgent)
})

object Utilities_UtilityInstallUnityEditorOnBuildAgent : BuildType({
    name = "☢️ Utility - Install Unity Editor on Build Agent"

    params {
        text("automation.UnityChangeset", "", label = "Unity changeset", display = ParameterDisplay.PROMPT, allowEmpty = false)
        text("automation.UnityModules", "windows-il2cpp", label = "Unity modules", display = ParameterDisplay.PROMPT, allowEmpty = true)
        checkbox("automation.UseUnityHub", "true", label = "Use Unity Hub?", description = "Hub CLI used to be unreliable.", display = ParameterDisplay.PROMPT,
                  checked = "true", unchecked = "false")
        text("automation.UnityVersion", "", label = "Unity version", display = ParameterDisplay.PROMPT, allowEmpty = false)
    }

    vcs {
        root(Core)

        checkoutMode = CheckoutMode.ON_AGENT
    }

    steps {
        script {
            name = "Install Unity Editor (cmd line)"
            workingDir = "%automation.WorkDir%"
            scriptContent = """dotnet run --config %automation.Config% installUnityEditor %automation.UnityVersion% %automation.UnityChangeset% --hub="%automation.UseUnityHub%" --modules="%automation.UnityModules%""""
        }
    }

    features {
        swabra {
            filesCleanup = Swabra.FilesCleanup.DISABLED
            lockingProcesses = Swabra.LockingProcessPolicy.REPORT
        }
    }

    requirements {
        exists("NBG_CAN_BUILD", "RQ_4")
    }
    
    disableSettings("RQ_4", "RUNNER_14")
})
