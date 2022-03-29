// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Net;
using ZeroInstall.Publish.Cli;
using ZeroInstall.Store.Implementations;
using ZeroInstall.Store.Trust;

ProcessUtils.SanitizeEnvironmentVariables();
NetUtils.ApplyProxy();

// Automatically show help for missing args
if (args.Length == 0) args = new[] {"--help"};

try
{
    using var handler = new AnsiCliTaskHandler();
    var command = (args.FirstOrDefault() == "capture")
        ? (ICommand)new CaptureCommand(args.Skip(1), handler)
        : new PublishCommand(args, handler);
    return (int)command.Execute();
}
#region Error hanlding
catch (OperationCanceledException)
{
    return (int)ExitCode.UserCanceled;
}
catch (ArgumentException ex)
{
    Log.Error(ex);
    return (int)ExitCode.InvalidArguments;
}
catch (OptionException ex)
{
    Log.Error(ex);
    return (int)ExitCode.InvalidArguments;
}
catch (WebException ex)
{
    Log.Error(ex);
    return (int)ExitCode.WebError;
}
catch (InvalidDataException ex)
{
    Log.Error(ex);
    return (int)ExitCode.InvalidData;
}
catch (IOException ex)
{
    Log.Error(ex);
    return (int)ExitCode.IOError;
}
catch (UnauthorizedAccessException ex)
{
    Log.Error(ex);
    return (int)ExitCode.AccessDenied;
}
catch (DigestMismatchException ex)
{
    Log.Info(ex.LongMessage);
    Log.Error(ex);
    return (int)ExitCode.DigestMismatch;
}
catch (KeyNotFoundException ex)
{
    Log.Error(ex);
    return (int)ExitCode.InvalidArguments;
}
catch (WrongPassphraseException ex)
{
    Log.Error(ex);
    return (int)ExitCode.InvalidArguments;
}
catch (NotSupportedException ex)
{
    Log.Error(ex);
    return (int)ExitCode.NotSupported;
        }
        #endregion
