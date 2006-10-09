echo off
echo ====================================
echo = 	Build and Convert the Microsoft.VisualBasic.dll into Java jar
echo =	
echo =	NOTE: firest, build the Microsoft.VisualBasic.dll using the TARGET_JVM=True define flag.
echo =	
echo =	sample usage: 
echo =	VB.J2EE.build.bat 2 debug
echo =	
echo ====================================

echo Get batch command parameters.
SET VB_BUILD_PARAM_NET_VERSION=%1
SET VB_BUILD_PARAM_CONFIGURATION=%2

echo Set command parameters default.
IF %VB_BUILD_PARAM_NET_VERSION%=="" SET VB_BUILD_PARAM_NET_VERSION=2
IF %VB_BUILD_PARAM_CONFIGURATION%=="" SET VB_BUILD_PARAM_CONFIGURATION=debug

echo Build the .NET assembly which will be converted into java.
SET VB_COMPILE_OPTIONS_J2EE=/define:TARGET_JVM=True
call VB.build.bat %VB_BUILD_PARAM_NET_VERSION% %VB_BUILD_PARAM_CONFIGURATION%
IF %ERRORLEVEL% NEQ 0 GOTO EXCEPTION

rem ====================================
rem set environment settings for running J2EE applications
IF NOT DEFINED JAVA_HOME SET JAVA_HOME="C:\jdk1.5.0_06"
echo using JAVA_HOME=%JAVA_HOME%


rem ===========================
rem = SET CLASSPATH and Java options
rem = 
rem = Grasshopper variables and jars
rem ===========================

IF %VB_BUILD_PARAM_NET_VERSION%=="1" ( 
SET VMW4J2EE_DIR=C:\Program Files\Mainsoft\Visual MainWin for J2EE
)
IF %VB_BUILD_PARAM_NET_VERSION%=="2" ( 
SET VMW4J2EE_DIR=C:\Program Files\Mainsoft\Visual MainWin for J2EE V2
)

SET VMW4J2EE_JGAC_DIR=jgac\vmw4j2ee_110

SET VMW4J2EE_JGAC_JARS="%VMW4J2EE_DIR%\%VMW4J2EE_JGAC_DIR%\mscorlib.jar"
SET VMW4J2EE_JGAC_JARS=%VMW4J2EE_JGAC_JARS%;"%VMW4J2EE_DIR%\%VMW4J2EE_JGAC_DIR%\System.jar"
SET VMW4J2EE_JGAC_JARS=%VMW4J2EE_JGAC_JARS%;"%VMW4J2EE_DIR%\%VMW4J2EE_JGAC_DIR%\System.Xml.jar"
SET VMW4J2EE_JGAC_JARS=%VMW4J2EE_JGAC_JARS%;"%VMW4J2EE_DIR%\%VMW4J2EE_JGAC_DIR%\System.Data.jar"
SET VMW4J2EE_JGAC_JARS=%VMW4J2EE_JGAC_JARS%;"%VMW4J2EE_DIR%\%VMW4J2EE_JGAC_DIR%\vmwutils.jar"
SET VMW4J2EE_JGAC_JARS=%VMW4J2EE_JGAC_JARS%;"%VMW4J2EE_DIR%\%VMW4J2EE_JGAC_DIR%\J2SE.Helpers.jar"


SET BUILD_NET_FRAMEWORK_DIR="%FRAMEWORKDIR%\%FRAMEWORKVERSION%"
echo using BUILD_NET_FRAMEWORK_DIR=%BUILD_NET_FRAMEWORK_DIR%
set path=%path%;%BUILD_NET_FRAMEWORK_DIR%

pushd bin
echo on
echo converting dll to jar without validator
"%VMW4J2EE_DIR%\bin\jcsc.exe" %CD%\Microsoft.VisualBasic.dll /novalidator /out:%CD%\Microsoft.VisualBasic.jar /classpath:%VMW4J2EE_JGAC_JARS%;%CD%\Microsoft.VisualBasic.jar /lib:%CD%;"%VMW4J2EE_DIR%\jgac\jre5";"%VMW4J2EE_DIR%\jgac"
IF %ERRORLEVEL% NEQ 0 GOTO EXCEPTION
echo running java validator
%JAVA_HOME%\bin\java.exe -cp .;..;"%VMW4J2EE_DIR%\bin\validator.jar";"%VMW4J2EE_DIR%\bin\bcel.jar";%VMW4J2EE_JGAC_JARS%;"%CD%\Microsoft.VisualBasic.jar" -Xms256m -Xmx512m validator.Validator -jar:"%CD%\Microsoft.VisualBasic.jar" 
IF %ERRORLEVEL% NEQ 0 GOTO EXCEPTION
popd



:FINALLY
echo ======================
echo finished
echo ======================
GOTO END

:EXCEPTION
echo ========================
echo ERROR --- Batch Terminated 
popd
echo ========================
PAUSE

:END
