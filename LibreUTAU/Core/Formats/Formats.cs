using System;
using System.IO;
using System.Windows;
using LibreUtau.Core.Commands;
using LibreUtau.Core.USTx;

namespace LibreUtau.Core.Formats {
    public enum ProjectFormat { Unknown, Vsq3, Vsq4, Ust, Ustx }

    static class Formats {
        const string ustMatch = "[#SETTING]";
        const string vsq3Match = VSQx.vsq3NameSpace;
        const string vsq4Match = VSQx.vsq4NameSpace;

        public static ProjectFormat DetectProjectFormat(string file) {
            if (!IsTextFile(file)) return ProjectFormat.Unknown;
            string contents = "";
            StreamReader streamReader = null;
            try {
                streamReader = File.OpenText(file);
                for (int i = 0; i < 10; i++) {
                    if (streamReader.Peek() < 0) break;
                    contents += streamReader.ReadLine();
                }
            } catch (Exception e) {
                if (streamReader != null) streamReader.Dispose();
                MessageBox.Show(e.GetType() + "\n" + e.Message);
                return ProjectFormat.Unknown;
            }

            if (contents.Contains(ustMatch)) return ProjectFormat.Ust;
            if (contents.Length > 0 && contents[0] == '{') return ProjectFormat.Ustx;
            if (contents.Contains(vsq3Match)) return ProjectFormat.Vsq3;
            if (contents.Contains(vsq4Match)) return ProjectFormat.Vsq4;
            return ProjectFormat.Unknown;
        }

        public static void LoadProject(string file) {
            ProjectFormat format = DetectProjectFormat(file);
            UProject project = null;

            if (format == ProjectFormat.Ustx) { project = USTx.Load(file); } else if (
                format == ProjectFormat.Vsq3 || format == ProjectFormat.Vsq4) { project = VSQx.Load(file); } else if (
                format == ProjectFormat.Ust) { project = Ust.Load(file); } else {
                MessageBox.Show("Unknown file format");
            }

            if (project != null) { CommandDispatcher.Inst.ExecuteCmd(new LoadProjectNotification(project)); }
        }

        public static bool IsTextFile(string file) {
            FileStream stream = null;
            try {
                FileInfo info = new FileInfo(file);
                if (info.Length > 8 * 1024 * 1024) return false;
                stream = info.OpenRead();
                byte[] data = new byte[1024];
                stream.Read(data, 0, 1024);
                int i = 1;
                while (i < 1024 && i < info.Length) {
                    if (data[i - 1] == 0 && data[i] == 0) return false;
                    i++;
                }

                return true;
            } catch {
                return false;
            } finally {
                if (stream != null) stream.Dispose();
            }
        }
    }
}
