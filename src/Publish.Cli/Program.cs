// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Security;
using NanoByte.Common.Net;
using ZeroInstall.Publish;
using ZeroInstall.Publish.Cli;
using ZeroInstall.Store.Implementations;
using ZeroInstall.Store.Trust;

ProcessUtils.SanitizeEnvironmentVariables();
NetUtils.ApplyProxy();
ServicePointManager.DefaultConnectionLimit = 16;

using var handler = new AnsiCliTaskHandler();

try
{
    // Automatically show help for missing args
    if (args.Length == 0) args = ["--help"];

    ICommand command = args switch
    {
        ["capture", ..] => new CaptureCommand(args.Skip(1), handler),
        _ => new PublishCommand(args, handler)
    };
    return (int)command.Execute();
}
#region Error handling
catch (OperationCanceledException)
{
    return (int)ExitCode.UserCanceled;
}
catch (Exception ex) when (ex is ArgumentException or OptionException or KeyNotFoundException or WrongPassphraseException)
{
    handler.Error(ex);
    return (int)ExitCode.InvalidArguments;
}
catch (WebException ex)
{
    handler.Error(ex);
    return (int)ExitCode.WebError;
}
catch (InvalidDataException ex)
{
    handler.Error(ex);
    return (int)ExitCode.InvalidData;
}
catch (IOException ex)
{
    handler.Error(ex);
    return (int)ExitCode.IOError;
}
catch (Exception ex) when (ex is UnauthorizedAccessException or SecurityException)
{
    handler.Error(ex);
    return (int)ExitCode.AccessDenied;
}
catch (DigestMismatchException ex)
{
    Log.Info(ex.LongMessage);
    handler.Error(ex);
    return (int)ExitCode.DigestMismatch;
}
catch (NotSupportedException ex)
{
    handler.Error(ex);
    return (int)ExitCode.NotSupported;
}
#endregion
