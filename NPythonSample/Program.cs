using System;
using NPythonCore;

namespace NPythonSample
{
    class Program
    {
        static void Main(string[] args)
        {
            //python.exeの場所を指定
            //Pythonの文字出力が不要な場合はStringReceivedを省略可能
            NPython nPython = new NPython(@"C:\Python27\python.exe", StringReceived);

            int[,] array =
            {
                {0,  1,  2,  3},
                {4,  5,  6,  7},
                {8,  9, 10, 11}
            };

            //arrayの内容をxとしてPythonに送信
            nPython.Send("x", array);

            //pythonで受信したxを表示する
            nPython.WriteLine("x");

            //xのすべての値に10を加算する
            nPython.WriteLine("x = x + 10");

            //加算したxを表示する
            nPython.WriteLine("x");

            //xをｙに転送
            nPython.WriteLine("y = x");

            //test.pyを読み込む
            nPython.WriteLine("import test");

            //test内の関数calcを呼び出す
            nPython.WriteLine("x = test.calc(x)");

            //関数の結果を表示する
            nPython.WriteLine("x");

            //計算したxをC#で取得
            Array resultX = nPython.Get("x");

            //Pythonで宣言したyをC#で取得
            Array resultY = nPython.Get("y");

            //取得した値を転送する
            int[,] destArrayX = new int[3, 4];
            Buffer.BlockCopy(resultX, 0, destArrayX, 0, sizeof(int) * resultX.Length);

            //取得した値を転送する
            int[,] destArrayY = new int[3, 4];
            Buffer.BlockCopy(resultY, 0, destArrayY, 0, sizeof(int) * resultY.Length);

            //取得したXの中身を表示
            for (int i = 0; i < destArrayX.GetLength(0); i++)
            {
                for (int j = 0; j < destArrayX.GetLength(1); j++)
                {
                    Console.WriteLine(i* destArrayX.GetLength(1) +j+ " : " + destArrayX[i, j]);
                }
            }

            //取得したYの中身を表示
            for (int i = 0; i < destArrayY.GetLength(0); i++)
            {
                for (int j = 0; j < destArrayY.GetLength(1); j++)
                {
                    Console.WriteLine(i * destArrayY.GetLength(1) + j + " : " + destArrayY[i, j]);
                }
            }

            //以後は対話モードとして動作する
            do
            {
                string inputText = Console.ReadLine();

                if (!nPython.HasExited)
                {
                    nPython.WriteLine(inputText);
                }

            } while (!nPython.HasExited);
        }

        //Pythonから文字列を取得したときの処理
        static void StringReceived(string str)
        {
            Console.WriteLine(str);
        }
    }
}
