◎任意のexeをサービスをとして実行する

　・複数のサービスを登録するときは、exe/configをコピーして使う。
　　・シンボリックリンク不可。


○コマンドライン
　Example:
　%windir%\Microsoft.NET\Framework64\v4.0.30319\InstallUtil.exe AnyExeService.exe
　%windir%\Microsoft.NET\Framework64\v4.0.30319\InstallUtil.exe /u AnyExeService.exe


○config
AnyExeService.exe.config
  /configuration/appSettings

    <add key="ServiceName" value="NameOfService"/>
    <add key="DisplayName" value="DisplayName of service"/>
    <add key="Description" value="Description of service"/>
    <!--
      Automatic
      Manual
      Disabled
    -->
    <add key="StartMode" value="Automatic"/>
    <!-- 
      LocalService
      NetworkService
      LocalSystem
      User：UserName/Passwordにログオンユーザを指定する。指定がないとGUIプロンプト（InstallUtil.exeから）。
    -->
    <add key="ServiceAccount" value="User"/>
    <add key="UserName" value=""/>
    <add key="Password" value=""/>
    <add key="Executable" value="notepad.exe"/>
    <add key="Argument" value=""/>
    <add key="WorkingDirectory" value=""/>


○ソース

  https://github.com/tckz/AnyExeService


○ライセンス

　SEE LICENSE.txt　（2条項BSDライセンス）


