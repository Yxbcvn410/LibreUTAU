#region

/*
    Resample主引擎调度类
    ResamplerEngine 接口用于实施具体的信息调度
    ResamplerAdapter.LoadEngine过程用于识别并调度引擎，若该文件为可用引擎则返回ResamplerEngine，否则返回null
    引擎DLL开发说明见ResamplerIOModels中
 */

#endregion

using System.Collections.Generic;
using System.IO;
using LibreUtau.Core.ResamplerDriver.Factories;

namespace LibreUtau.Core.ResamplerDriver {
    public interface IResamplerDriver {
        Stream DoResampler(DriverModels.EngineInput args);
        DriverModels.EngineInfo GetInfo();
    }

    internal class ResamplerDriver {
        public static IResamplerDriver LoadEngine(string filePath) {
            if (!File.Exists(filePath))
                return null;

            IResamplerDriver ret = null;

            switch (Path.GetExtension(filePath)?.ToLower()) {
                case ".exe":
                    ret = new ExeDriver(filePath);
                    break;
                case ".dll":
                    CppDriver retcpp = new CppDriver(filePath);
                    if (retcpp.isLegalPlugin) {
                        ret = retcpp;
                    } else {
                        SharpDriver retnet = new SharpDriver(filePath);
                        if (retnet.isLegalPlugin) {
                            ret = retnet;
                        }
                    }

                    break;
            }

            return ret;
        }

        public static IEnumerable<DriverModels.EngineInfo> SearchEngines(string path) {
            var engineInfoList = new List<DriverModels.EngineInfo>();
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            var files = Directory.EnumerateFiles(path);
            foreach (var file in files) {
                var engine = LoadEngine(file);
                if (engine != null) engineInfoList.Add(engine.GetInfo());
            }

            return engineInfoList;
        }
    }
}
