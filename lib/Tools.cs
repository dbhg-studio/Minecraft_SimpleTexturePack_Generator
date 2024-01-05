using AdonisUI.Controls;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;

namespace MinecraftResourcePack_Builder.lib
{
    class Tools
    {
        /// <summary>
        /// 文件夹是否存在
        /// </summary>
        /// <param name="folderPath">文件夹的路径</param>
        /// <returns>
        /// 如果 folderPath 为空或文件夹不存在失败 false，则返回 true 。
        /// </returns>
        public bool Isfolder(string folderPath)
        {
            if (folderPath == null || folderPath == "")
            {
                return false;
            }
            else
            {
                return Directory.Exists(folderPath);
            }
        }

        /// <summary>
        /// 使用默认浏览器打开链接
        /// </summary>
        /// <param name="url">链接</param>
        /// <returns>
        /// url 为空、格式错误或打开失败都返回 false，否则返回 true 。
        /// </returns>
        public bool OpenLink(string url)
        {
            string pattern = @"^((https|http|ftp|rtsp|mms)?:\/\/)[^\s]+";
            Regex regex = new Regex(pattern);
            bool isUrl = regex.IsMatch(url);
            if (isUrl == false)
            {
                return false;
            }
            try
            {
                Process.Start(url);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 判断图片是否为1:1
        /// </summary>
        /// <param name="imagePath">图片路径</param>
        /// <returns>
        /// imagePath 为空与图片不为1:1都返回 false，否则返回 true 。
        /// </returns>
        public bool IsImage(string imagePath)
        {
            if (imagePath == null || imagePath == "")
            {
                return false;
            }
            BitmapImage bitmapImage = new BitmapImage(new Uri(imagePath));
            int width = bitmapImage.PixelWidth;
            int height = bitmapImage.PixelHeight;

            if (width == height)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 项目路径
        /// </summary>
        /// <param name="name">项目名称</param>
        /// <param name="path">子目录</param>
        /// <param name="pathlist">子目录组</param>
        /// <returns>
        /// name | path | pathlist 如果为空则返回主路径，如果不为空则拼接作为路径返回
        /// </returns>
        public string ProjectPath(string name = null, string path = null, string[] pathlist = null)
        {
            string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;

            if (pathlist != null)
            {
                string result = $@"{currentDirectory}Project\{name}\";

                for (int i = 0; i < pathlist.Length; i++)
                {
                    result += $"{pathlist[i]}";
                    if (i < pathlist.Length - 1)
                    {
                        result += "\\";
                    }
                }

                return result;
            }
            if (path != null)
            {
                return $@"{currentDirectory}Project\{name}\{path}";
            }
            if (name != null)
            {
                return $@"{currentDirectory}Project\{name}";
            }
            return $@"{currentDirectory}Project";
        }

        /// <summary>
        /// 项目打包路径
        /// </summary>
        /// <param name="name">项目名称</param>
        /// <returns>
        /// name 如果为空则返回主路径，如果不为空则拼接作为路径返回
        /// </returns>
        public string BuildPath(string name)
        {
            string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            return $@"{currentDirectory}Project\{name}\dist";
        }

        /// <summary>
        /// 模板路径
        /// </summary>
        /// <param name="version">版本编号</param>
        /// <returns>
        /// path 如果为空则返回主路径，如果不为空则拼接作为路径返回
        /// </returns>
        public string TemplatePath(string version = null)
        {
            string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            if (version != null)
            {
                return $@"{currentDirectory}Template\{version}";
            }
            return $@"{currentDirectory}Template";
        }

        /// <summary>
        /// 创建项目文件夹
        /// </summary>
        /// <param name="folderName">项目名称</param>
        /// <returns>
        /// folderName 如果为空或创建文件夹失败则返回 false ，否则返回 true 。
        /// </returns>
        public bool CreateProjectFolder(string folderName)
        {
            if (folderName == null || folderName == "")
            {
                return false;
            }
            string folderPath = Path.Combine(ProjectPath(), folderName);
            string Dist = Path.Combine(ProjectPath(folderName), "dist");
            string Src = Path.Combine(ProjectPath(folderName), "src");
            string Assets = Path.Combine(ProjectPath(folderName, "src"), "assets");
            string Minecraft = Path.Combine(ProjectPath(folderName, @"src\assets"), "minecraft");
            string Textures = Path.Combine(ProjectPath(folderName, @"src\assets\minecraft"), "textures");

            try
            {
                Directory.CreateDirectory(folderPath);
                Directory.CreateDirectory(Dist);
                Directory.CreateDirectory(Src);
                Directory.CreateDirectory(Assets);
                Directory.CreateDirectory(Minecraft);
                Directory.CreateDirectory(Textures);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 删除项目文件夹
        /// </summary>
        /// <param name="folderName">项目名称</param>
        /// <returns>
        /// 删除成功返回 true，否则 false 。
        /// </returns>
        public bool DeleteProjectFolder(string folderName)
        {
            if (folderName == null || folderName == "")
            {
                return false;
            }
            string folderPath = Path.Combine(ProjectPath(), folderName);
            if (Directory.Exists(folderPath))
            {
                try
                {
                    Directory.Delete(folderPath, true);
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 打包项目文件夹
        /// </summary>
        /// <param name="ProjectName">项目名称</param>
        /// <returns>
        /// 启动打包程序
        /// </returns>
        public void BuildProject(string ProjectName)
        {
            if (ProjectName == null)
            {
                MessageBox.Show("项目名称为空", "打包程序错误");
                return;
            }
            // 指定需要打包的文件夹路径
            string startPath = ProjectPath(ProjectName, "src");

            // 指定压缩后的zip文件保存路径
            string zipPath = $@"{BuildPath(ProjectName)}\{ProjectName}.zip";

            Building building = new Building(startPath, BuildPath(ProjectName), zipPath);
            building.ShowDialog();
        }

        /// <summary>
        /// 创建Mcmeta文件
        /// </summary>
        /// <param name="name">项目名称</param>
        /// <param name="version">项目版本编号</param>
        /// <param name="Introduction">项目简介</param>
        /// <returns>
        ///  创建成功返回 true，否则 false。
        /// </returns>
        public bool CreateMcmeta(string name, string version, string Introduction = null)
        {
            if (name == null && version == null || name == "" && version == "")
            {
                return false;
            }

            // 定义要替换和插入的内容
            string replacement = @"{
    ""pack"": {
        ""pack_format"": {version},
        ""description"": ""{Introduction}""
    }
}";
            // 替换内容
            string replacedContent = replacement.Replace("{version}", version)
                                                 .Replace("{Introduction}", Introduction);
            if (Isfolder(ProjectPath(name, "src")))
            {
                // 创建文件
                string filePath = Path.Combine(ProjectPath(name, "src"), "pack.mcmeta");
                try
                {
                    File.WriteAllText(filePath, replacedContent);
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }


        }

        /// <summary>
        /// 读取Mcmeta文件
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <returns>
        ///  输出pack_format的值
        /// </returns>
        public string Mcmeta(string path)
        {
            string jsonContent = File.ReadAllText(path);
            var json = JObject.Parse(jsonContent);
            int packFormat = json["pack"]["pack_format"].Value<int>();
            return packFormat.ToString();
        }

        /// <summary>
        /// 创建项目Pack图片
        /// </summary>
        /// <param name="path">图片路径</param>
        /// <param name="name">项目名称</param>
        /// <param name="overwrite">是否覆盖性创建</param>
        /// <returns>
        ///  创建成功返回 true，否则 false。
        /// </returns>
        public bool CreatePack(string path, string name, bool overwrite = false)
        {
            if (path == null || path == "")
            {
                return false;
            }

            // 获取文件的扩展名
            string fileExtension = Path.GetExtension(path);
            // 构建新文件路径
            string newFilePath = Path.Combine(ProjectPath(name, "src"), "pack" + fileExtension);

            try
            {
                // 复制文件
                File.Copy(path, newFilePath, overwrite);

                // 确保文件已经复制成功
                if (File.Exists(newFilePath))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }


        }

        /// <summary>
        /// 自定义消息弹窗
        /// </summary>
        public MessageBoxModel CustomizeMessage(string text, string content, MessageBoxImage icon = MessageBoxImage.None)
        {
            var messageBox = new MessageBoxModel
            {
                Text = content,
                Caption = text,
                Icon = icon,
                Buttons = new[]
                {
                    MessageBoxButtons.Ok("确定"),
                    MessageBoxButtons.Cancel("取消"),
                },
                IsSoundEnabled = false,
            };
            return messageBox;
        }


    }
}
