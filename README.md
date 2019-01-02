# NPython
NPythonは開発を終了し[NConstrictor](https://github.com/harujoh/NConstrictor)へ移行しています。
NConstrictorは値のコンバートを行わず、標準出力を介さずにメモリを直接やり取りする手法を採っています。

## できること
- PythonにC#のArrayを送信
- C#からPythonのndArrayを取得

## 動作原理
- C#からPythonを対話モードで起動
- 起動したPythonの出力をStandardOutputを介して取得
- データのやり取りはnpyを文字列として送受信
