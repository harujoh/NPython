using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace NPythonCore
{
    public class NPython : IDisposable
    {
        private readonly StreamWriter _sw;
        readonly Process _process;

        public bool HasExited => _process.HasExited;

        private readonly AutoResetEvent _writeWait = new AutoResetEvent(false);
        private readonly AutoResetEvent _readWait = new AutoResetEvent(true);
        public Action<string> StringReceived;

        //Pythonからのデータ吸出し用バッファ
        private string _bufffer;

        //python.exeの場所を指定
        public NPython(string pythonPath, Action<string> stringReceived = null, string mainpy = "")
        {
            if (stringReceived == null)
            {
                //出力取得メソッドに指定がない時は値を破棄
                StringReceived = (string str) => { };
            }
            else
            {
                StringReceived = stringReceived;
            }

            _process = new Process
            {
                StartInfo =
                {
                    FileName = pythonPath,
                    Arguments = @"-i " + mainpy,  // インタラクティブモードを強制する
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    CreateNoWindow = true
                }
            };

            _process.OutputDataReceived += DataReceived;
            _process.Start();
            _process.BeginOutputReadLine();

            _sw = _process.StandardInput;
            _sw.WriteLine("import numpy as np\nimport io");
        }

        public void WriteLine(string str)
        {
            _sw.WriteLine(str);
        }

        public void Send(string name, Array array)
        {
            byte[] b = NpyFormat.Save(array);
            string str = "\"\\x" + BitConverter.ToString(b).Replace("-", "\\x") + "\"";

            _sw.WriteLine("content = " + str);
            _sw.WriteLine(name + " = np.load(io.BytesIO(content))");
        }

        public Array Get(string name)
        {
            _sw.WriteLine("ioBytes = io.BytesIO()\nnp.save(ioBytes," + name + ")\nioBytes.getvalue()");

            //DataReceivedでバッファが書かれるのを待つ
            _writeWait.WaitOne();
            byte[] b = Encoding.GetEncoding("iso-8859-1").GetBytes(Regex.Unescape(_bufffer.Replace("\"", "")));
            _bufffer = null;

            //DataReceivedに読み込み完了を通知
            _readWait.Set();

            Array result = NpyFormat.LoadMatrix(new MemoryStream(b));

            return result;
        }

        //イベント呼び出しは一行に一回なので多次元配列の出力等では何回も呼ばれる
        void DataReceived(object sender, DataReceivedEventArgs e)
        {
            //先頭文字でバッファ取得要求を判断
            int index = e.Data.IndexOf(@"""\x93NUMPY\x01\x00");
            if (index >= 0)
            {
                //読み込み中なら書き込みを待つ
                _readWait.WaitOne();

                _bufffer = e.Data;

                //Getにバッファの書き込みを通知
                _writeWait.Set();
            }
            else
            {
                StringReceived(e.Data);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _sw.Close();
            }

            _process.CancelOutputRead();
            _process.WaitForExit();
            _process.Close();
        }

        ~NPython()
        {
            Dispose(false);
        }
    }
}
