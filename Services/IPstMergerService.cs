using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PSTFuze.Services;

public interface IPstMergerService
{
    Task MergePstFilesAsync(IList<string> sourcePstPaths, string outputPath, IProgress<(double percentage, string message)> progress, CancellationToken cancellationToken);
}