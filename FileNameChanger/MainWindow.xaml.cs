using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FileNameChanger
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        #region 数据

        private bool isCancelCurTask = false;
        private bool isStartTask = false;
        private bool isSelectRegexVaild = false;
        private bool isChangeRegexVaild = false;

        public Dictionary<string, FileInfo> changeFileDic = new Dictionary<string, FileInfo>();
        public List<FileInfo> curAllFileInfoList = new List<FileInfo>();
        public List<FileInfo> curSelectFileInfoList = new List<FileInfo>();

        public List<string> curAllFileList = new List<string>();
        #endregion

        public MainWindow()
        {
            InitializeComponent();
            ShowTip("请将一个文件夹拖入应用窗口");
        }

        #region 窗口方法

        private void Window_PreviewDrop(object sender, DragEventArgs e)
        {
            curAllFileInfoList.Clear();
            curAllFileList.Clear();
            curSelectFileInfoList.Clear();
            changeFileDic.Clear();

            string[] dropPaths = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (var item in dropPaths)
            {
                if (!Directory.Exists(item))
                {
                    ShowTip($"该文件不是一个文件夹,请重新选择：{item}");
                    return;
                }
            }

            ShowTip($"成功识别所选文件夹，默认读取里面的所有文件类型");

            StartTaskGetAllFiles(dropPaths, OnGetAllFilesComplete, OnGetAllFileCancel);

        }

        private void Window_PreviewDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Link;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void CanelTask_Click(object sender, RoutedEventArgs e)
        {
            if(isStartTask)
            {
                isCancelCurTask = true;
                ShowTip("任务正在取消");
            }
            else
            {
                ShowTip("当前无正在进行的任务");
            }
        }

        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            SetTargetContent(curAllFileList);
            ShowTip("当前选择所有类型文件");
            curSelectFileInfoList.Clear();
            curSelectFileInfoList.AddRange(curAllFileInfoList);
        }

        private void SelectTypeButton_Click(object sender, RoutedEventArgs e)
        {
            string typePattern = selectTypeText.Text;
            if (string.IsNullOrEmpty(typePattern))
            {
                ShowTip("当前选择空类型文件");
                SelectTargetFilesByType(new List<string>() { "" });
            }
            else
            {
                string[] types = typePattern.Split(',');
                SelectTargetFilesByType(types.ToList());
            }
        }

        private void SelectRegexText_TextChanged(object sender, TextChangedEventArgs e)
        {
            RegexTest(selectRegexTextInput, selectRegexText);
        }

        private void ChangeRegexInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            RegexTest(changeRegexInput, changeRegexText);
        }

        private void SelectRegexButton_Click(object sender, RoutedEventArgs e)
        {
            if(isSelectRegexVaild)
            {
                ShowTip("筛选成功");

                bool isAll = false;
                SelectTargetFilesByRegex(GetRe(selectRegexTextInput,out isAll));
            }
            else
            {
                ShowTip("正则表达式错误,需采用javescript正则风格");
            }
        }


        private void ChangeRegexButton_Click(object sender, RoutedEventArgs e)
        {
            if (isChangeRegexVaild)
            {
                ShowTip("添加成功，效果如预览所示，效果如预期则点击确定更改");

                bool isAll = false;
                ChangeTargetFilesByRegex(GetRe(changeRegexInput,out isAll), isAll);
            }
            else
            {
                ShowTip("正则表达式错误,需采用javescript正则风格");
            }
        }

        private void ChangeBackButton_Click(object sender, RoutedEventArgs e)
        {
            string addString = changeBeforeBackInput.Text;
            if(string.IsNullOrEmpty(addString))
            {
                ShowTip("前缀后缀为空");
                return;
            }
            else
            {
                ChangeBeforeOrBackFiles(addString, false);
                ShowTip("添加成功，效果如预览所示，效果如预期则点击确定更改");
            }
        }


        private void ChangeBeforeButton_Click(object sender, RoutedEventArgs e)
        {
            string addString = changeBeforeBackInput.Text;
            if (string.IsNullOrEmpty(addString))
            {
                ShowTip("前缀后缀为空");
                return;
            }
            else
            {
                ChangeBeforeOrBackFiles(addString, true);
                ShowTip("添加成功，效果如预览所示，效果如预期则点击确定更改");
            }
        }

        private void SavePathButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in changeFileDic)
            {
                if (item.Value.FullName == $"{item.Value.DirectoryName}\\{item.Key}")
                {
                    continue;
                }
                else
                {
                    File.Copy(item.Value.FullName, $"{item.Value.DirectoryName}\\{item.Key}", true);
                    File.Delete(item.Value.FullName);
                }
            }
            ShowTip("替换完成");
            changeFileDic.Clear();
            SetOutPutContent(new List<string>());
            SetTargetContent(new List<string>());
        }

        private void SaveToButton_Click(object sender, RoutedEventArgs e)
        {
            using (CommonOpenFileDialog dialog = new CommonOpenFileDialog())
            {
                dialog.IsFolderPicker = true;//设置为选择文件夹
                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    string savePath = dialog.FileName;
                    foreach (var item in changeFileDic)
                    {
                        File.Copy(item.Value.FullName, $"{savePath}\\{item.Key}", true);
                    }
                    ShowTip("替换完成");
                    changeFileDic.Clear();
                    SetOutPutContent(new List<string>());
                    SetTargetContent(new List<string>());
                }
            }
        }

        private void selectTargetToView_Click(object sender, RoutedEventArgs e)
        {
            changeFileDic.Clear();
            List<string> changeList = new List<string>();
            foreach (var item in curSelectFileInfoList)
            {
                changeList.Add(item.Name);
                changeFileDic.Add(item.Name, item);
            }
            SetOutPutContent(changeList);
        }
        #endregion

        #region 线程

        public void StartTaskGetAllFiles(string[] pathArr, Action<List<string>> onComplete,Action onCancel)
        {
            isStartTask = true;

            isCancelCurTask = false;
            ShowTip("正在读取文件夹下的所有文件，如果时间过长，可取消本次任务");
            ThreadPool.QueueUserWorkItem(o =>
            {
                List<string> allFileName = new List<string>();
                foreach (var item in pathArr)
                {
                    List<string> curList = GetAllFiles(new DirectoryInfo(item));
                    allFileName.AddRange(curList);
                }

                if(isCancelCurTask)
                {
                    this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, (Action)delegate () { onCancel?.Invoke(); });
                }
                else
                {
                    this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, (Action)delegate () { onComplete?.Invoke(allFileName); });
                }

                isStartTask = false;
            });
        }

        #endregion

        #region 操作方法

        private Regex GetRe(TextBox box,out bool isAll)
        {
            string regex = box.Text;

            int first = regex.IndexOf('/');
            if (first > -1)
            {
                regex = regex.Remove(0, first + 1);
            }

            isAll = false;
            bool isIgnoreCase = false;
            int end = regex.LastIndexOf('/');
            if (end > -1)
            {
                string type = regex.Substring(end);
                if (type.IndexOf("i") > -1)
                {
                    isIgnoreCase = true;
                }
                if (type.IndexOf("g") > -1)
                {
                    isAll = true;
                }
                regex = regex.Remove(end);
            }

            Regex re;
            if (isIgnoreCase)
            {
                re = new Regex(regex, RegexOptions.IgnoreCase);
            }
            else
            {
                re = new Regex(regex);
            }

            return re;
        }

        private void RegexTest(TextBox box, Label label)
        {
            string regex = box.Text;

            int first = regex.IndexOf('/');
            if (first > -1)
            {
                regex = regex.Remove(0, first + 1);
            }

            int end = regex.LastIndexOf('/');
            if (end > -1)
            {
                regex = regex.Remove(end);
            }

            //合法性检验
            try
            {
                Regex re = new Regex(regex);
                SetRegexTextTip(label, true);
            }
            catch (Exception ex)
            {
                SetRegexTextTip(label, false);
            }
        }

        private void ChangeBeforeOrBackFiles(string addString,bool isBefore)
        {
            changeFileDic.Clear();
            List<string> changeList = new List<string>();
            foreach (var item in curSelectFileInfoList)
            {
                string changeName = "";
                if (isBefore)
                {
                    changeName = addString + item.Name;
                    changeList.Add(changeName);
                }
                else
                {
                    
                    if(string.IsNullOrEmpty(item.Extension))
                    {
                        changeName = item.Name + addString;
                        changeList.Add(changeName);
                    }
                    else
                    {
                        changeName = item.Name.Replace(item.Extension, "") + addString + item.Extension;
                        changeList.Add(changeName);
                    }

                    if (changeFileDic.ContainsKey(changeName))
                    {
                        ShowTip($"{changeName}:更改名称重复，操作失败");
                        return;
                    }
                }

                changeFileDic.Add(changeName, item);
            }
            SetOutPutContent(changeList);
        }

        private void ChangeTargetFilesByRegex(Regex re,bool isAll)
        {
            changeFileDic.Clear();
            List<string> changeList = new List<string>();
            foreach (var item in curSelectFileInfoList)
            {
                string changeName = "";
                if (isAll)
                {
                    changeName = re.Replace(item.Name, changeRegexRepleceContent.Text);
                }
                else
                {
                    changeName = re.Replace(item.Name, changeRegexRepleceContent.Text,1);
                }
                
                if(string.IsNullOrEmpty(changeName) || !string.IsNullOrEmpty(item.Extension) && string.IsNullOrEmpty(changeName.Replace(item.Extension,"")))
                {
                    ShowTip($"{item.Name}:被更改为空名称，操作失败");
                    return;
                }
                changeList.Add(changeName);
                if(changeFileDic.ContainsKey(changeName))
                {
                    ShowTip($"{changeName}:更改名称重复，操作失败");
                    return;
                }
                changeFileDic.Add(changeName, item);
            }
            SetOutPutContent(changeList);
        }

        private void SelectTargetFilesByRegex(Regex re)
        {
            List<FileInfo> selectList = new List<FileInfo>();
            List<string> fixTypeList = new List<string>();
            foreach (var item in curSelectFileInfoList)
            {
                if (re.IsMatch(item.Name))
                {
                    fixTypeList.Add(item.Name);
                    selectList.Add(item);
                }
            }

            curSelectFileInfoList.Clear();
            curSelectFileInfoList.AddRange(selectList);

            SetTargetContent(fixTypeList);
        }

        private void SetRegexTextTip(Label label,bool isVaild)
        {
            label.Content = isVaild ? "有效" : "无效";
            //合法赋值
            if(label == selectRegexText)
            {
                isSelectRegexVaild = isVaild;
            }
            else if(label == changeRegexText)
            {
                isChangeRegexVaild = isVaild;
            }
        }

        private void SelectTargetFilesByType(List<string> typePattern)
        {
            List<FileInfo> selectList = new List<FileInfo>();
            List<string> fixTypeList = new List<string>();
            foreach (var item in curSelectFileInfoList)
            {
                if(typePattern.IndexOf(item.Extension) > -1)
                {
                    fixTypeList.Add(item.Name);
                    selectList.Add(item);
                }
            }

            curSelectFileInfoList.Clear();
            curSelectFileInfoList.AddRange(selectList);

            SetTargetContent(fixTypeList);
        }

        public void SetTargetContent(List<string> nameList)
        {
            targetList.ItemsSource = nameList;
        }

        public void SetOutPutContent(List<string> nameList)
        {
            output_list.ItemsSource = nameList;
        }

        private void OnGetAllFilesComplete(List<string> nameList)
        {
            ShowTip("任务操作完成");
            SetTargetContent(nameList);
            curAllFileList = nameList;
        }

        private void OnGetAllFileCancel()
        {
            ShowTip("任务被取消");
        }

        public List<string> GetAllFiles(DirectoryInfo curPath)
        {
            //线程被终止
            if(isCancelCurTask)
            {
                return new List<string>();
            }

            List<string> pathList = new List<string>();

            FileInfo[] allFile = curPath.GetFiles();
            foreach (FileInfo fileItem in allFile)
            {
                pathList.Add(fileItem.Name);
                curAllFileInfoList.Add(fileItem);
                curSelectFileInfoList.Add(fileItem);
            }

            DirectoryInfo[] allDir = curPath.GetDirectories();
            foreach (DirectoryInfo dirItem in allDir)
            {
                List<string> nextList = GetAllFiles(dirItem);
                pathList.AddRange(nextList);
            }

            return pathList;
        }

        public void ShowTip(string message)
        {
            tip.Text = message;
        }

        #endregion


    }
}
