using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using Tesseract;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
namespace WpfApp1
{
    public class MeanFilter
    {
        private const int BufferSize = 128;
        private const int TrimSize = 16;
        private readonly List<double> values = new List<double>();

        public double Update(double newValue)
        {
            // 添加新值
            values.Add(newValue);

            // 如果超过了缓冲区大小，删除最旧的值
            if (values.Count > BufferSize)
            {
                values.RemoveAt(0);
            }

            // 获取所有值并排序
            var sortedValues = values.OrderBy(x => x).ToList();

            // 如果值数量不足128，直接排序取中间值返回
            if (values.Count < BufferSize)
            {
                return sortedValues[sortedValues.Count / 2];
            }

            // 否则，去掉最大的16个和最小的16个值，然后计算平均值
            var trimmedValues = sortedValues.Skip(TrimSize).Take(BufferSize - 2 * TrimSize).ToList();
            return trimmedValues.Average();
        }
    }

    public partial class MainWindow : Window
    {
        private Thread ocrThread;
        private Thread lifeThread;
        private Thread manaThread;
        private bool isRunning = true;
        private bool isKeepLife = false;
        private bool isKeepmage = false;
        private double lifePercentage;
        private double shieldPercentage;
        private double magePercentage;
        private double spiritPercentage;
        private double totalLife = 0;

        private readonly Random random = new Random();
        private CancellationTokenSource Shiel_cancellationTokenSource;


        private CancellationTokenSource qiyuan_cancellationTokenSource;
        private CancellationTokenSource Skillloops_cancellationTokenSource;
        

        // 定义一个只读字段，保存 16 组坐标
        private static readonly List<Tuple<int, int>> coordinates = new List<Tuple<int, int>>
        {
            Tuple.Create(350, 2115),
            Tuple.Create(410, 2062),
            Tuple.Create(440, 1980),
            Tuple.Create(446, 1925),
            Tuple.Create(420, 1850),
            Tuple.Create(400, 1820),
            Tuple.Create(375, 1796),
            Tuple.Create(340, 1773),
            Tuple.Create(300, 1755),
            Tuple.Create(268, 1750)
        };


        public MainWindow()
        {
            InitializeComponent();
          //  StartOCRThread();  // 启动 OCR 线程
           // StartLifeThread();  // 启动生命值监控线程
          //  StartManaThread();  // 启动魔力值监控线程
        }


        private void ShielCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            // 当 CheckBox 被选中时执行的代码
            // MessageBox.Show("CheckBox 被选中");
            // 比如可以启用 Slider
            // ValueSlider.IsEnabled = true; // 启用 Slider

            // 创建一个 CancellationTokenSource
            Shiel_cancellationTokenSource = new CancellationTokenSource();
            var token = Shiel_cancellationTokenSource.Token;

            // 启动一个 Task 来运行护盾 Keep 线程
            Task.Run(() => ShieldKeep(token));

        }
        private void ShielCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            // 当 CheckBox 被取消选中时执行的代码
            //MessageBox.Show("CheckBox 被取消选中");
            // 比如可以禁用 Slider
            //  ValueSlider.IsEnabled = false; // 禁用 Slider

            Shiel_cancellationTokenSource?.Cancel();
        }



        private void qiyuanCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            // 当 CheckBox 被选中时执行的代码
            // MessageBox.Show("CheckBox 被选中");
            // 比如可以启用 Slider
            // ValueSlider.IsEnabled = true; // 启用 Slider

            // 创建一个 CancellationTokenSource
            qiyuan_cancellationTokenSource = new CancellationTokenSource();
            var token = qiyuan_cancellationTokenSource.Token;

            // 启动一个 Task 来运行护盾 Keep 线程
            Task.Run(() => qiyuanKeep(token));

        }


        private void qiyuanCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            // 当 CheckBox 被取消选中时执行的代码
            //MessageBox.Show("CheckBox 被取消选中");
            // 比如可以禁用 Slider
            //  ValueSlider.IsEnabled = false; // 禁用 Slider

            qiyuan_cancellationTokenSource?.Cancel();
        }





        private void Skillloops_Checked(object sender, RoutedEventArgs e)
        {
            // 当 CheckBox 被选中时执行的代码
            // MessageBox.Show("CheckBox 被选中");
            // 比如可以启用 Slider
            // ValueSlider.IsEnabled = true; // 启用 Slider
          //  Thread.Sleep(5000);
            // 创建一个 CancellationTokenSource
            Skillloops_cancellationTokenSource = new CancellationTokenSource();
            var token = Skillloops_cancellationTokenSource.Token;

            // 启动一个 Task 来运行护盾 Keep 线程
            Task.Run(() => Skillloops(token));

        }


        private void Skillloops_Unchecked(object sender, RoutedEventArgs e)
        {
            // 当 CheckBox 被取消选中时执行的代码
            //MessageBox.Show("CheckBox 被取消选中");
            // 比如可以禁用 Slider
            //  ValueSlider.IsEnabled = false; // 禁用 Slider

            Skillloops_cancellationTokenSource?.Cancel();
        }

        private static DateTime qiyuan_time = DateTime.MinValue;
        private void ShieldKeep(CancellationToken token)
        {
            try
            {
                while (true)
                {
                    // 检查是否被取消
                    token.ThrowIfCancellationRequested();

                    // 在这里执行任务，比如模拟护盾的工作
                    Console.WriteLine("护盾正在运行...");

                    // 在 Dispatcher 上读取 Slider 的值
                    int index = (int)Dispatcher.Invoke(() => ShielLevel.Value);





                    // 确保不超出 coordinates 的范围
                    if (index >= 0 && index < coordinates.Count)
                    {
                        var coord = coordinates[index];

                        int color = GetColorAt(coord.Item1, coord.Item2);
                        if (color <0x80) {

                            KeyboardSimulator.SimulateKeyPress(0x31);
                           // Trace.WriteLine($"At ({coord.Item1}, {coord.Item2}), Color: {color:X}");
                           Thread.Sleep(1800); // 暂停 1 秒
                        }
                        
                    }

                    // 模拟一些工作
                    Thread.Sleep(50); // 暂停 1 秒
                }
            }
            catch (OperationCanceledException)
            {
                // 捕获取消请求
                Console.WriteLine("护盾已停止。");
            }
        }

    
        private void Skillloops(CancellationToken token)
        {
            //try
            //{
            //    while (true)
            //    {
            //        // 检查是否被取消
            //        token.ThrowIfCancellationRequested();

            //        int delay = 1000; // 默认值
            //        Application.Current.Dispatcher.Invoke(() =>
            //        {
            //            if (!int.TryParse(skillloopsDelay.Text, out delay))
            //            {
            //                delay = 1000; // 如果转换失败，使用默认值 1000
            //            }
            //        });

            //        // 计算当前时间与 qiyuan_time 的差值（毫秒）
            //        double timeDiff = (DateTime.Now - qiyuan_time).TotalMilliseconds;

            //        // 如果差值大于设定的延迟值，则发送 E 键
            //       var life = GetColorAt(240, 1970);
            //       // Trace.WriteLine($"life : {life}");
            //        if (life != 53) { 
            //            continue;
            //        }


            //        var life_drg = GetColorAt(507, 2080);
            //        Trace.WriteLine($"life_drg : {life_drg}");
            //        if (life != 53)
            //        {
            //            continue;
            //        }

            //        if (timeDiff > delay)
            //        {
            //           KeyboardSimulator.SimulateKeyPress(0x45); // 'E' 键的虚拟键码
            //            Thread.Sleep(500);
            //        }
            //        // 延迟指定的毫秒数
            //        Thread.Sleep(100);

                  
            //    }
            //}
            //catch (OperationCanceledException)
            //{
            //    // 捕获取消请求
            //    Console.WriteLine("护盾已停止。");
            //}
        }


        private void qiyuanKeep(CancellationToken token)
        {
            try
            {
                while (true)
                {
                    // 检查是否被取消
                    token.ThrowIfCancellationRequested();



                    // 在 Dispatcher 上读取 Slider 的值
                   
                       //     KeyboardSimulator.SimulateKeyPress(0x31);
                    Bitmap screenshot = SaveScreenRegion(2972, 2048, 3016, 2083);
                    var skill_lum = GetLuam(screenshot);

                    //var life = GetColorAt(240, 1970);
                    ////Trace.WriteLine($"life : {life}");
                    //if ((life != 47)&&(life != 94))
                    //{
                    //    continue;
                    //}


                    var life_drg = GetColorAt(507, 2100);
                   // Trace.WriteLine($"life_drg : {life_drg}");
                    if (life_drg < 31)
                    {
                       // continue;
                    }

                    int delay = 1000; // 默认值
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (!int.TryParse(skillloopsDelay.Text, out delay))
                        {
                            delay = 1000; // 如果转换失败，使用默认值 1000
                        }
                    });




                    // 计算当前时间与 qiyuan_time 的差值（毫秒）
                    double timeDiff = (DateTime.Now - qiyuan_time).TotalMilliseconds;

                    //if ((timeDiff > delay*1.5))
                    //{
                    //    KeyboardSimulator.SimulateKeyPress(0x45); // E 键的虚拟键码
                    //    qiyuan_time = DateTime.Now; // 记录当前时间 
                    //}

                    // 在 Dispatcher 上读取 Slider 的值
                    int index = (int)Dispatcher.Invoke(() => ShielLevel.Value);
                    var coord = coordinates[index];

                    int color = GetColorAt(coord.Item1, coord.Item2);

                    //  if ((skill_lum > 130)&&(timeDiff > delay))
                    if ((skill_lum > 130) && (color>0x80))
                    {
                        KeyboardSimulator.SimulateKeyPress(0x57); // W 键的虚拟键码
                        qiyuan_time = DateTime.Now; // 记录当前时间 
                        Thread.Sleep(50);
                    }
                    Thread.Sleep(5); 
                }
            }
            catch (OperationCanceledException)
            {
                // 捕获取消请求
                Console.WriteLine("护盾已停止。");
            }
        }
        // P/Invoke to call GetPixel
        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);


        [DllImport("gdi32.dll")]
        private static extern uint GetPixel(IntPtr hdc, int nXPos, int nYPos);

        [DllImport("user32.dll")]
        private static extern void ReleaseDC(IntPtr hWnd, IntPtr hdc);

        private int GetColorAt(int x, int y)
        {
            IntPtr desk = GetDesktopWindow();
            IntPtr dc = GetWindowDC(desk);
            int pixelColor = (int)GetPixel(dc, x, y);
            ReleaseDC(desk, dc);
            // 提取 RGB 组件
            int r = (pixelColor & 0x00FF0000) >> 16; // 红色组件
            int g = (pixelColor & 0x0000FF00) >> 8;  // 绿色组件
            int b = (pixelColor & 0x000000FF);       // 蓝色组件

            // 计算灰度值
            int gray = (int)(0.299 * r + 0.587 * g + 0.114 * b);
            return gray;
        }
        //private void Button_Click(object sender, RoutedEventArgs e)
        //{
        //    isKeepLife = !isKeepLife;
        //    life.Content = isKeepLife ? "Stop" : "Start";
        //    Trace.WriteLine($"isKeepLife: {isKeepLife}");
        //}

        //private void Button_Click_1(object sender, RoutedEventArgs e)
        //{
        //    isKeepmage = !isKeepmage;
        //    mage.Content = isKeepmage ? "Stop" : "Start";
        //}

        private void StartOCRThread()
        {
            ocrThread = new Thread(() =>
            {
                while (isRunning)
                {
                    try
                    {
                        UpdateLifeAndShield();
                        UpdateMageAndSpirit();
                        Thread.Sleep(10);
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine($"Error: {ex.Message}");
                    }
                }
            });

            ocrThread.IsBackground = true;
            ocrThread.Start();
            Trace.WriteLine("OCR thread started.");
        }

        private void UpdateLifeAndShield()
        {
            Bitmap screenshot = SaveScreenRegion(200, 1630, 425, 1720);
            string ocrResult = PerformOCR(screenshot);
            ocrResult = ocrResult.Replace(",", "");
            string[] lines = ocrResult.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var filter1 = new MeanFilter();
            if (lines.Length >= 2)
            {
                string[] lifeParts = lines[0].Split('/');
                if (lifeParts.Length == 2)
                {
                    double currentLife = double.Parse(lifeParts[0]);
                    double recognizedTotalLife = double.Parse(lifeParts[1]);

                    double filteredTotalLife = filter1.Update(recognizedTotalLife);
                  //  Trace.WriteLine($"OCR life  {filteredTotalLife} recognizedTotalLife {recognizedTotalLife}.");
                    if (Math.Abs(filteredTotalLife - recognizedTotalLife) > 50)
                    {
                        Trace.WriteLine($"OCR life  {filteredTotalLife} recognizedTotalLife {recognizedTotalLife}.");
                        return;
                    }

                    totalLife = filteredTotalLife;
                    lifePercentage = currentLife / totalLife;
                }

                shieldPercentage = ParseLineToPercentage(lines[1]);
            }
        }

        private void UpdateMageAndSpirit()
        {
            Bitmap screenshot = SaveScreenRegion(3535, 1500, 3731, 1720);
            string ocrResult = PerformOCR(screenshot);
            string[] lines2 = ocrResult.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var filter1 = new MeanFilter();
            if (lines2.Length >= 2)
            {



                string[] lifeParts = lines2[0].Split('/');
                if (lifeParts.Length == 2)
                {
                    double currentMana= double.Parse(lifeParts[0]);
                    double recognizedTotaMana= double.Parse(lifeParts[1]);

                    double filteredTotalMana = filter1.Update(recognizedTotaMana);
                    //  Trace.WriteLine($"OCR life  {filteredTotalLife} recognizedTotalLife {recognizedTotalLife}.");
                    if (Math.Abs(filteredTotalMana - recognizedTotaMana) > 50)
                    {
                        Trace.WriteLine($"OCR mana  {recognizedTotaMana} fillter  {filteredTotalMana}.");
                        return;
                    }


                }

                magePercentage = ParseLineToPercentage(lines2[0]);
                spiritPercentage = ParseLineToPercentage(lines2[1]);



            }
        }

        private void StartLifeThread()
        {
            lifeThread = new Thread(() =>
            {
                double lastLifePercentage = 0;
                DateTime lastOutputTime = DateTime.MinValue;
                while (isRunning)
                {
                    try
                    {
                        Thread.Sleep(100 + random.Next(0, 50));

                        double currentLifePercentage = lifePercentage;
                        TimeSpan timeSinceLastOutput = DateTime.Now - lastOutputTime;

                        if (Math.Abs(currentLifePercentage - lastLifePercentage) < double.Epsilon)
                        {
                            continue;
                        }

                        if (Math.Abs(currentLifePercentage - lastLifePercentage) > double.Epsilon || timeSinceLastOutput.TotalSeconds >= 5)
                        {
                            lastOutputTime = DateTime.Now;
                            lastLifePercentage = currentLifePercentage;
                        }

                        double keepLine = 0.9;
                        if (currentLifePercentage > keepLine || currentLifePercentage < 0.02 || !isKeepLife)
                        {
                            continue;
                        }

                        currentLifePercentage = keepLine - currentLifePercentage;
                        currentLifePercentage = currentLifePercentage * 1.5;

                        if (lifePercentage < 0.65)
                        {
                            KeyboardSimulator.SimulateKeyPress(0x31);
                            Thread.Sleep(random.Next(3000, 4000));
                            Trace.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] , life {lifePercentage} key 1 pressed with probability {currentLifePercentage}");
                            continue;
                        }
                        
                        //if (random.NextDouble() < currentLifePercentage)
                        //{
                        //    KeyboardSimulator.SimulateKeyPress(0x31);
                        //    Thread.Sleep(random.Next(3000, 400));
                        //    Trace.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] , life {currentLifePercentage} key 1 pressed with probability {currentLifePercentage}");
                        //}
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine($"Error: {ex.Message}");
                    }
                }
            });

            lifeThread.IsBackground = true;
            lifeThread.Start();
            Trace.WriteLine("Life monitoring thread started.");
        }

        private void StartManaThread()
        {
            manaThread = new Thread(() =>
            {
                double lastMagePercentage = 0;
                DateTime lastOutputTime = DateTime.MinValue;
                while (isRunning)
                {
                    try
                    {
                        double currentMagePercentage = magePercentage;
                        Thread.Sleep(random.Next(500, 700));
                        TimeSpan timeSinceLastOutput = DateTime.Now - lastOutputTime;

                        if (Math.Abs(currentMagePercentage - lastMagePercentage) < double.Epsilon)
                        {
                            continue;
                        }

                        if (timeSinceLastOutput.TotalSeconds >= 5)
                        {
                            lastOutputTime = DateTime.Now;
                            lastMagePercentage = currentMagePercentage;
                        }

                        if (!isKeepmage || currentMagePercentage < 0.05)
                        {
                            continue;
                        }

                        if (currentMagePercentage < 0.2)
                        {
                            KeyboardSimulator.SimulateKeyPress(0x32);
                            Trace.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] , mana {currentMagePercentage}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine($"Error: {ex.Message}");
                    }
                }
            });

            manaThread.IsBackground = true;
            manaThread.Start();
            Trace.WriteLine("Mana monitoring thread started.");
        }

        private double ParseLineToPercentage(string line)
        {
            string cleanedLine = Regex.Replace(line, @"[^\d/]", "");
            string[] parts = cleanedLine.Split('/');
            if (parts.Length == 2 && double.TryParse(parts[0], out double currentValue) && double.TryParse(parts[1], out double totalValue) && totalValue != 0)
            {
                return currentValue / totalValue;
            }
            return 0.0;
        }

        private Bitmap SaveScreenRegion(int x1, int y1, int x2, int y2)
        {
            int screenWidth = GetSystemMetrics(0);
            int screenHeight = GetSystemMetrics(1);

            int targetWidth = 3840;
            int targetHeight = 2160;

            double scaleX = (double)screenWidth / targetWidth;
            double scaleY = (double)screenHeight / targetHeight;

            int scaledX1 = (int)(x1 * scaleX);
            int scaledY1 = (int)(y1 * scaleY);
            int scaledX2 = (int)(x2 * scaleX);
            int scaledY2 = (int)(y2 * scaleY);

            int width = scaledX2 - scaledX1;
            int height = scaledY2 - scaledY1;

            Bitmap bitmap = new Bitmap(width, height);

            using (Graphics g = Graphics.FromImage(bitmap))
            {
                IntPtr hdcDestination = g.GetHdc();
                IntPtr hdcSource = GetWindowDC(GetDesktopWindow());
                BitBlt(hdcDestination, 0, 0, width, height, hdcSource, scaledX1, scaledY1, CopyPixelOperation.SourceCopy);
                ReleaseDC(GetDesktopWindow(), hdcSource);
                g.ReleaseHdc(hdcDestination);
            }

            //bitmap.Save("filePath.png", System.Drawing.Imaging.ImageFormat.Png);
            return bitmap;
        }

        public string PerformOCR(Bitmap image)
        {
            using var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default);
            engine.SetVariable("tessedit_pageseg_mode", "6");
            engine.SetVariable("tessedit_char_whitelist", "0123456789/,");

            using var memoryStream = new MemoryStream();
            image.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Bmp);
            memoryStream.Position = 0;

            using var img = Pix.LoadFromMemory(memoryStream.ToArray());
            using var page = engine.Process(img);

            return page.GetText();
        }

        public int GetLuam(Bitmap image)
        {

            int width = image.Width;
            int height = image.Height;
            long totalGray = 0;
            int pixelCount = width * height;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var pixelColor = image.GetPixel(x, y);
                    // 使用加权平均法计算灰度值
                    int grayValue = (int)(0.3 * pixelColor.R + 0.59 * pixelColor.G + 0.11 * pixelColor.B);
                    totalGray += grayValue;
                }
            }

            // 计算平均灰度值
            int averageGray = (int)(totalGray / pixelCount);
            return averageGray; // 返回平均灰度值
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hObject, int nXDest, int nYDest, int nWidth, int nHeight,
            IntPtr hObjectSource, int nXSrc, int nYSrc, CopyPixelOperation SourceCopy);



        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        private double LifePercentage
        {
            get { return lifePercentage; }
            set { lifePercentage = value; }
        }

        private double ShieldPercentage
        {
            get { return shieldPercentage; }
            set { shieldPercentage = value; }
        }

        private double MagePercentage
        {
            get { return magePercentage; }
            set { magePercentage = value; }
        }

        private double SpiritPercentage
        {
            get { return spiritPercentage; }
            set { spiritPercentage = value; }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            isRunning = false;
        }

        private void MyCheckBoxskillSqen_Checked(object sender, RoutedEventArgs e)
        {

        }
    }

    public static class KeyboardSimulator
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public int type;
            public InputUnion u;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public KEYBDINPUT ki;
            [FieldOffset(0)] public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public int mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        private const int INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        [DllImport("user32.dll")]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        public static void SimulateKeyPress(ushort keyCode)
        {
            INPUT[] inputs = new INPUT[2];

            inputs[0].type = INPUT_KEYBOARD;
            inputs[0].u.ki.wVk = keyCode;
            inputs[0].u.ki.dwFlags = 0;

            inputs[1].type = INPUT_KEYBOARD;
            inputs[1].u.ki.wVk = keyCode;
            inputs[1].u.ki.dwFlags = KEYEVENTF_KEYUP;

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }
    }
}
