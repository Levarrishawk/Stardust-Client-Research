try
{
    LocomotionEventPreviewScenarios.Run();
    if(args.Length==2)LocomotionEventPreviewScenarios.Original(args[0],args[1]);
    else if(args.Length!=0)throw new ArgumentException("Expected optional archive root and configuration.");
}
catch(Exception failure){Console.Error.WriteLine(failure);Environment.ExitCode=1;}
