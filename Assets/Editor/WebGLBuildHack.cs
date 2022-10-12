#if UNITY_EDITOR_OSX
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

// Needed for Unity 2020.3 on macOS 12.3.1, but not needed for Unity 2022.1
// https://forum.unity.com/threads/case-1412113-builderror-osx-12-3-and-unity-2020-3-constant-build-errors.1255419/#post-8038196
public class WebGLHack : IPreprocessBuildWithReport {

    public int callbackOrder => 1;

    public void OnPreprocessBuild(BuildReport report) {
        System.Environment.SetEnvironmentVariable("EMSDK_PYTHON", "/Library/Frameworks/Python.framework/Versions/2.7/bin/python2.7");
    }
}
#endif
