#r @"FAKE.3.5.4/tools/FakeLib.dll"
#load "build-helpers.fsx"
open Fake
open System
open System.IO
open System.Linq
open BuildHelpers
open Fake.XamarinHelper

Target "core-build" (fun () ->
    RestorePackages "ResilientServices.Core.sln"

    MSBuild "ResilientServices/ResilientServices/bin/Debug" "Build" [ ("Configuration", "Debug"); ("Platform", "Any CPU") ] [ "ResilientServices.Core.sln" ] |> ignore
)

Target "core-tests" (fun () -> 
    RunNUnitTests "ResilientServices/ResilientServices/bin/Debug/TipCalc.Tests.dll" "ResilientServices/ResilientServices/bin/Debug/testresults.xml"
)

Target "ios-build" (fun () ->
    RestorePackages "TipCalc.iOS.sln"

    iOSBuild (fun defaults ->
        {defaults with
            ProjectPath = "TipCalc.iOS.sln"
            Configuration = "Debug|iPhoneSimulator"
            Target = "Build"
        })
)

Target "ios-adhoc" (fun () ->
    RestorePackages "TipCalc.iOS.sln"

    iOSBuild (fun defaults ->
        {defaults with
            ProjectPath = "TipCalc.iOS.sln"
            Configuration = "Ad-Hoc|iPhone"
            Target = "Build"
        })

    let appPath = Directory.EnumerateFiles(Path.Combine("src", "TipCalc.iOS", "bin", "iPhone", "Ad-Hoc"), "*.ipa").First()

    TeamCityHelper.PublishArtifact appPath
)

Target "ios-appstore" (fun () ->
    RestorePackages "TipCalc.iOS.sln"

    iOSBuild (fun defaults ->
        {defaults with
            ProjectPath = "TipCalc.iOS.sln"
            Configuration = "AppStore|iPhone"
            Target = "Build"
        })

    let outputFolder = Path.Combine("src", "TipCalc.iOS", "bin", "iPhone", "AppStore")
    let appPath = Directory.EnumerateDirectories(outputFolder, "*.app").First()
    let zipFilePath = Path.Combine(outputFolder, "TipCalc.iOS.zip")
    let zipArgs = String.Format("-r -y '{0}' '{1}'", zipFilePath, appPath)

    Exec "zip" zipArgs

    TeamCityHelper.PublishArtifact zipFilePath
)

Target "ios-uitests" (fun () ->
    let appPath = Directory.EnumerateDirectories(Path.Combine("src", "TipCalc.iOS", "bin", "iPhoneSimulator", "Debug"), "*.app").First()

    RunUITests appPath
)

Target "ios-testcloud" (fun () ->
    RestorePackages "TipCalc.iOS.sln"

    iOSBuild (fun defaults ->
        {defaults with
            ProjectPath = "TipCalc.iOS.sln"
            Configuration = "Debug|iPhone"
            Target = "Build"
        })

    let appPath = Directory.EnumerateFiles(Path.Combine("src", "TipCalc.iOS", "bin", "iPhone", "Debug"), "*.ipa").First()

    getBuildParam "devices" |> RunTestCloudTests appPath
)

Target "android-build" (fun () ->
    RestorePackages "TipCalc.Android.sln"

    MSBuild "ResilientServices/ResilientServices.Android/bin/Release" "Build" [ ("Configuration", "Release") ] [ "TipCalc.Android.sln" ] |> ignore
)

Target "android-package" (fun () ->
    AndroidPackage (fun defaults ->
        {defaults with
            ProjectPath = "ResilientServices/ResilientServices.Android/TipCalc.Android.csproj"
            Configuration = "Release"
            OutputPath = "ResilientServices/ResilientServices.Android/bin/Release"
        }) 
    |> AndroidSignAndAlign (fun defaults ->
        {defaults with
            KeystorePath = "tipcalc.keystore"
            KeystorePassword = "tipcalc" // TODO: don't store this in the build script for a real app!
            KeystoreAlias = "tipcalc"
        })
    |> fun file -> TeamCityHelper.PublishArtifact file.FullName
)

Target "android-uitests" (fun () ->
    AndroidPackage (fun defaults ->
        {defaults with
            ProjectPath = "ResilientServices/ResilientServices.Android/TipCalc.Android.csproj"
            Configuration = "Release"
            OutputPath = "ResilientServices/ResilientServices.Android/bin/Release"
        }) |> ignore

    let appPath = Directory.EnumerateFiles(Path.Combine("src", "TipCalc.Android", "bin", "Release"), "*.apk", SearchOption.AllDirectories).First()

    RunUITests appPath
)

Target "android-testcloud" (fun () ->
    AndroidPackage (fun defaults ->
        {defaults with
            ProjectPath = "ResilientServices/ResilientServices.Android/TipCalc.Android.csproj"
            Configuration = "Release"
            OutputPath = "ResilientServices/ResilientServices.Android/bin/Release"
        }) |> ignore

    let appPath = Directory.EnumerateFiles(Path.Combine("src", "TipCalc.Android", "bin", "Release"), "*.apk", SearchOption.AllDirectories).First()

    getBuildParam "devices" |> RunTestCloudTests appPath
)

"core-build"
  ==> "core-tests"

"ios-build"
  ==> "ios-uitests"

"android-build"
  ==> "android-uitests"

"android-build"
  ==> "android-testcloud"

"android-build"
  ==> "android-package"

RunTarget() 