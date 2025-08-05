using UnityEditor;
using UnityEngine;

public static class BuildScript
{
    public static void BuildAndroid()
    {
        // 设置构建目标为 Android
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

        // 配置构建选项
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[] { "Assets/Scenes/SampleScene.unity" };
        buildPlayerOptions.locationPathName = "Builds/Android/AndroidBuild.apk";
        buildPlayerOptions.target = BuildTarget.Android;
        buildPlayerOptions.options = BuildOptions.None;

        // 执行构建
        BuildPipeline.BuildPlayer(buildPlayerOptions);
    }
}
