# NPython
C#からPythonとデータをやり取りする

## できること
- PythonにC#のArrayを送信
- C#からPythonのndArrayを取得

## 原理
- C#からPythonを対話モードで起動
- 起動したPythonの出力をStandardOutputを介して取得
- データのやり取りはnpyを文字列として送受信

## メリット
- Pythonの全機能をC#を介して使用できる
Pythonライブラリの全機能が有効になるためCypthonやGPUを活用した高速な処理を期待できる
