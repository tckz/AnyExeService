���C�ӂ�exe���T�[�r�X���Ƃ��Ď��s����

�@�E�����̃T�[�r�X��o�^����Ƃ��́Aexe/config���R�s�[���Ďg���B
�@�@�E�V���{���b�N�����N�s�B


���R�}���h���C��
�@Example:
�@%windir%\Microsoft.NET\Framework64\v4.0.30319\InstallUtil.exe AnyExeService.exe
�@%windir%\Microsoft.NET\Framework64\v4.0.30319\InstallUtil.exe /u AnyExeService.exe


��config
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
      User�FUserName/Password�Ƀ��O�I�����[�U���w�肷��B�w�肪�Ȃ���GUI�v�����v�g�iInstallUtil.exe����j�B
    -->
    <add key="ServiceAccount" value="User"/>
    <add key="UserName" value=""/>
    <add key="Password" value=""/>
    <add key="Executable" value="notepad.exe"/>
    <add key="Argument" value=""/>
    <add key="WorkingDirectory" value=""/>


���\�[�X

  https://github.com/tckz/AnyExeService


�����C�Z���X

�@SEE LICENSE.txt�@�i2����BSD���C�Z���X�j


