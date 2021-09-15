using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

/*
##performancetestresult2:{"SampleGroups":[{"Name":"Simple","Unit":2,"IncreaseIsBetter":false,"Samples":[4.6000000000000488E-05,1.9000000000000126E-05,1.5999999999998237E-05,1.4999999999998349E-05,1.6000000000000457E-05,1.9000000000000126E-05,1.7000000000000349E-05,1.6000000000000457E-05,1.6000000000000457E-05,1.6000000000000457E-05,1.5000000000000568E-05,1.6000000000000457E-05,1.6000000000000457E-05,1.5999999999998237E-05,1.5999999999998237E-05,1.5000000000000568E-05,1.6000000000000457E-05,1.7000000000000349E-05,1.7000000000000349E-05,1.6000000000000457E-05],"Min":1.4999999999998349E-05,"Max":4.6000000000000488E-05,"Median":1.6000000000000457E-05,"Average":1.7799999999999979E-05,"StandardDeviation":6.5543878432697117E-06,"Sum":0.0003559999999999996},{"Name":"LowerCase","Unit":2,"IncreaseIsBetter":false,"Samples":[2.6000000000000049E-05,2.4999999999999954E-05,2.3999999999999994E-05,0.00018400000000000005,2.3999999999999994E-05,2.6000000000000049E-05,2.3999999999999994E-05,2.3999999999999994E-05,2.2999999999999966E-05,2.8999999999999997E-05,2.8000000000000108E-05,2.3999999999999994E-05,2.5000000000000022E-05,2.3999999999999994E-05,2.3999999999999994E-05,2.3999999999999994E-05,2.3999999999999994E-05,2.3999999999999994E-05,2.3999999999999994E-05,2.4000000000000133E-05],"Min":2.2999999999999966E-05,"Max":0.00018400000000000005,"Median":2.3999999999999994E-05,"Average":3.2700000000000022E-05,"StandardDeviation":3.4740610242193518E-05,"Sum":0.0006540000000000005},{"Name":"UpperCase","Unit":2,"IncreaseIsBetter":false,"Samples":[2.5000000000000022E-05,2.5000000000000022E-05,2.4999999999999988E-05,2.4999999999999954E-05,2.5000000000000022E-05,2.3999999999999994E-05,2.3999999999999994E-05,2.3999999999999994E-05,2.4000000000000062E-05,2.3999999999999994E-05,2.5000000000000022E-05,2.5000000000000022E-05,2.5000000000000022E-05,2.3999999999999994E-05,2.4000000000000062E-05,2.3999999999999994E-05,2.3999999999999994E-05,2.3999999999999994E-05,2.5000000000000022E-05,2.3999999999999994E-05],"Min":2.3999999999999994E-05,"Max":2.5000000000000022E-05,"Median":2.4000000000000062E-05,"Average":2.4450000000000005E-05,"StandardDeviation":4.97493718553312E-07,"Sum":0.00048900000000000007},{"Name":"Complex","Unit":2,"IncreaseIsBetter":false,"Samples":[3.3000000000000009E-05,3.7999999999999975E-05,3.2999999999999975E-05,3.2999999999999975E-05,3.2000000000000012E-05,3.3000000000000043E-05,3.2000000000000012E-05,3.2000000000000012E-05,3.2999999999999975E-05,3.2999999999999975E-05,3.2999999999999975E-05,3.3000000000000043E-05,3.2000000000000087E-05,3.2000000000000087E-05,3.2000000000000087E-05,3.2000000000000087E-05,3.2999999999999975E-05,3.2999999999999975E-05,3.1999999999999944E-05,3.1999999999999944E-05],"Min":3.1999999999999944E-05,"Max":3.7999999999999975E-05,"Median":3.2999999999999975E-05,"Average":3.2800000000000004E-05,"StandardDeviation":1.2884098726724983E-06,"Sum":0.00065600000000000012}],"Name":"Hydrogen.Tests.Editor.StringsTests.Measure_GetLowerCaseHashCode","Version":"1","Categories":["Performance","Hydrogen.Performance"]}
##performancetestruninfo2:{"TestSuite":"Editmode","Date":1590015565252,"Player":{"Development":true,"ScreenWidth":1024,"ScreenHeight":768,"ScreenRefreshRate":60,"Fullscreen":false,"Vsync":1,"AntiAliasing":2,"Batchmode":true,"RenderThreadingMode":"GraphicsJobs","GpuSkinning":false,"Platform":"WindowsEditor","ColorSpace":"Linear","AnisotropicFiltering":"ForceEnable","BlendWeights":"FourBones","GraphicsApi":"Direct3D11","ScriptingBackend":"IL2CPP","AndroidTargetSdkVersion":"AndroidApiLevelAuto","AndroidBuildSystem":"Gradle","BuildTarget":"StandaloneWindows64","StereoRenderingPath":"MultiPass"},"Hardware":{"OperatingSystem":"Windows 10  (10.0.0) 64bit","DeviceModel":"NUC8i7HVK (Intel(R) Client Systems)","DeviceName":"POND","ProcessorType":"Intel(R) Core(TM) i7-8809G CPU @ 3.10GHz","ProcessorCount":8,"GraphicsDeviceName":"Intel(R) HD Graphics 630","SystemMemorySizeMB":32686},"Editor":{"Version":"2020.1.0b9","Branch":"2020.1/release","Changeset":"9c0aec301c8d","Date":1589291438},"Dependencies":["com.unity.2d.sprite@1.0.0","com.unity.addressables@1.8.3","com.unity.analytics@3.3.5","com.unity.build-report-inspector@0.1.2-preview","com.unity.burst@1.3.0-preview.13","com.unity.collab-proxy@2.1.0-preview.3","com.unity.collections@0.8.0-preview.5","com.unity.dots.editor@0.6.0-preview","com.unity.entities@0.10.0-preview.6","com.unity.ide.rider@2.0.3","com.unity.ide.visualstudio@2.0.1","com.unity.ide.vscode@1.2.0","com.unity.inputsystem@1.0.0","com.unity.jobs@0.2.9-preview.15","com.unity.kinematica@0.5.0-preview.1","com.unity.memoryprofiler@0.2.3-preview.2","com.unity.performance.profile-analyzer@0.6.0-preview.1","com.unity.physics@0.3.2-preview","com.unity.platforms@0.4.0-preview.3","com.unity.platforms.desktop@0.4.0-preview.3","com.unity.platforms.linux@0.4.0-preview.3","com.unity.platforms.macos@0.4.0-preview.3","com.unity.platforms.windows@0.4.0-preview.3","com.unity.postprocessing@2.3.0","com.unity.probuilder@4.3.0-preview.9","com.unity.progrids@3.0.3-preview.6","com.unity.properties.ui@1.2.0-preview","com.unity.quicksearch@1.6.0-preview.8","com.unity.remote-config@1.3.1-preview.4","com.unity.rendering.hybrid@0.5.0-preview.6","com.unity.scriptablebuildpipeline@1.7.2","com.unity.searcher@4.2.0","com.unity.serialization@1.2.0-preview","com.unity.settings-manager@1.0.2","com.unity.shadergraph@8.0.1","com.unity.test-framework@1.1.14","com.unity.test-framework.performance@2.1.0-preview","com.unity.textmeshpro@3.0.0-preview.13","com.unity.timeline@1.4.0-preview.5","com.unity.ugui@1.0.0","com.unity.ui.builder@0.11.2-preview","nuget.mono-cecil@0.1.6-preview","com.unity.modules.ai@1.0.0","com.unity.modules.androidjni@1.0.0","com.unity.modules.animation@1.0.0","com.unity.modules.assetbundle@1.0.0","com.unity.modules.audio@1.0.0","com.unity.modules.cloth@1.0.0","com.unity.modules.director@1.0.0","com.unity.modules.imageconversion@1.0.0","com.unity.modules.imgui@1.0.0","com.unity.modules.jsonserialize@1.0.0","com.unity.modules.particlesystem@1.0.0","com.unity.modules.physics@1.0.0","com.unity.modules.physics2d@1.0.0","com.unity.modules.screencapture@1.0.0","com.unity.modules.terrain@1.0.0","com.unity.modules.terrainphysics@1.0.0","com.unity.modules.tilemap@1.0.0","com.unity.modules.ui@1.0.0","com.unity.modules.uielements@1.0.0","com.unity.modules.umbra@1.0.0","com.unity.modules.unityanalytics@1.0.0","com.unity.modules.unitywebrequest@1.0.0","com.unity.modules.unitywebrequestassetbundle@1.0.0","com.unity.modules.unitywebrequestaudio@1.0.0","com.unity.modules.unitywebrequesttexture@1.0.0","com.unity.modules.unitywebrequestwww@1.0.0","com.unity.modules.vehicles@1.0.0","com.unity.modules.video@1.0.0","com.unity.modules.vr@1.0.0","com.unity.modules.wind@1.0.0","com.unity.modules.xr@1.0.0"],"Results":[]}
 */
namespace K9.Reports.Results
{
    public class PerformanceResult : IResult
    {
        public List<PerformanceTestResultSample> Samples = new List<PerformanceTestResultSample>();
        
        public DateTime Timestamp { get; set; }
        public string FullName { get; set; }
        public string Category { get; set; }
        public Agent Runner { get; set; }
        
        

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("==> ");
            sb.AppendLine(FullName);
            foreach (var s in Samples) sb.AppendLine(s.ToString());
            return sb.ToString();
        }

        #region IResult

        public string GetSheetName()
        {
            return Runner.Name;
        }
        public string GetCategory()
        {
            return Category;
        }

        public ResultType GetResultType()
        {
            return ResultType.Measurement;
        }
        
        public string GetName()
        {
            return FullName;
        }
        
        public DataTable GetTable(bool objectsAsStrings = false)
        {
            DataTable table = new DataTable();
            
            table.Columns.Add("Timestamp", objectsAsStrings ? typeof(string) : typeof(DateTime));
            table.Columns.Add("Agent", typeof(string));
            table.Columns.Add("Changelist", typeof(int));
            table.Columns.Add("Sample", typeof(string));
            table.Columns.Add("Median", typeof(float));
            table.Columns.Add("Minimum", typeof(float));
            table.Columns.Add("Maximum", typeof(float));
            table.Columns.Add("Average", typeof(float));
            table.Columns.Add("Standard Deviation", typeof(float));
            table.Columns.Add("Sample Count", typeof(int));
            table.Columns.Add("Sum", typeof(float));

            foreach (var s in Samples)
            {
                s.AddDataRow(table, this, objectsAsStrings);
            }
            
            return table;
        }

        #endregion
        

        public class PerformanceTestResultSample
        {
            public string Name;
            public float Average;
            public float Maximum;
            public float Median;
            public float Minimum;
            public int SampleCount;
            public float StandardDeviation;
            public float Sum;
            
            public override string ToString()
            {
                return
                    $"[{Name}] Median: {Median}ns | Minimum: {Minimum}ns | Maximum: {Maximum}ns | Average: {Average}ns | Standard Deviation: {StandardDeviation}ns | Sample Count: {SampleCount} | Total Time: {Sum}ns";
            }

            public void AddDataRow(DataTable table, PerformanceResult result, bool objectsAsStrings = false)
            {
                table.Rows.Add(
                    objectsAsStrings ? (object) result.Timestamp.ToString(Core.TimeFormat) : result.Timestamp,
                    result.Runner.Name,
                    Core.Changelist,
                    $"{result.GetName()}.{Name}", 
                    Median,
                    Minimum,
                    Maximum,
                    Average,
                    StandardDeviation,
                    SampleCount,
                    Sum);
            }
        }

    }
}