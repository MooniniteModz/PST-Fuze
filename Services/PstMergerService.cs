using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Redemption;

namespace PSTFuze.Services;

public class PstMergerService : IPstMergerService
{
    public async Task MergePstFilesAsync(IList<string> sourcePstPaths, string outputPath, 
        IProgress<(double percentage, string message)> progress, CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            try
            {
                // Initial setup
                progress.Report((0, "Starting merge process..."));
                System.Diagnostics.Debug.WriteLine("Starting PST merge process");
                
                cancellationToken.ThrowIfCancellationRequested();

                // Create session
                progress.Report((1, "Creating Redemption session..."));
                System.Diagnostics.Debug.WriteLine("Creating RDOSession");
                var session = new RDOSession();

                try
                {
                    // Create target PST
                    progress.Report((5, $"Opening target PST at {outputPath}"));
                    System.Diagnostics.Debug.WriteLine($"Opening target PST: {outputPath}");
                    
                    session.LogonPstStore(outputPath);
                    System.Diagnostics.Debug.WriteLine("Target PST opened successfully");
                    
                    var targetStore = session.Stores[session.Stores.Count];
                    System.Diagnostics.Debug.WriteLine("Got target store");
                    
                    var targetRootFolder = targetStore.RootFolder;
                    System.Diagnostics.Debug.WriteLine("Got target root folder");

                    var totalFiles = sourcePstPaths.Count;
                    var processedFiles = 0;

                    foreach (var sourcePath in sourcePstPaths)
                    {
                        System.Diagnostics.Debug.WriteLine($"Processing source PST: {sourcePath}");
                        cancellationToken.ThrowIfCancellationRequested();

                        try
                        {
                            progress.Report(((processedFiles * 100.0 / totalFiles), 
                                $"Starting to process: {Path.GetFileName(sourcePath)}"));

                            if (session.LoggedOn)
                            {
                                System.Diagnostics.Debug.WriteLine("Logging off previous session");
                                session.Logoff();
                            }

                            // Open source PST
                            System.Diagnostics.Debug.WriteLine($"Logging into source PST: {sourcePath}");
                            session.LogonPstStore(sourcePath);
                            System.Diagnostics.Debug.WriteLine("Source PST opened successfully");

                            var sourceStore = session.Stores[session.Stores.Count];
                            var sourceRootFolder = sourceStore.RootFolder;
                            System.Diagnostics.Debug.WriteLine("Got source root folder");

                            // Copy folders structure
                            System.Diagnostics.Debug.WriteLine("Starting folder copy process");
                            foreach (RDOFolder sourceFolder in sourceRootFolder.Folders)
                            {
                                System.Diagnostics.Debug.WriteLine($"Processing folder: {sourceFolder.Name}");
                                cancellationToken.ThrowIfCancellationRequested();
                                CopyFolder(sourceFolder, targetRootFolder, progress, processedFiles, totalFiles, cancellationToken);
                            }

                            // Copy root items
                            progress.Report(((processedFiles * 100.0 / totalFiles), 
                                $"Copying root items from {Path.GetFileName(sourcePath)}"));
                            
                            System.Diagnostics.Debug.WriteLine("Starting root messages copy");
                            CopyMessages(sourceRootFolder, targetRootFolder, progress, processedFiles, totalFiles, cancellationToken);

                            session.Logoff();
                            processedFiles++;
                            
                            progress.Report((processedFiles * 100.0 / totalFiles, 
                                $"Completed processing {Path.GetFileName(sourcePath)}"));
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error processing PST: {ex}");
                            progress.Report(((processedFiles * 100.0 / totalFiles), 
                                $"Error processing {Path.GetFileName(sourcePath)}: {ex.Message}"));
                            throw;
                        }
                    }

                    if (session.LoggedOn)
                    {
                        session.Logoff();
                    }

                    progress.Report((100, "PST merge completed successfully"));
                }
                finally
                {
                    if (session.LoggedOn)
                    {
                        try
                        {
                            session.Logoff();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error during final logoff: {ex}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Critical error: {ex}");
                progress.Report((0, $"Critical error during PST merge: {ex.Message}"));
                throw;
            }
        }, cancellationToken);
    }

    private void CopyFolder(RDOFolder sourceFolder, RDOFolder targetParentFolder, 
        IProgress<(double percentage, string message)> progress, int processedFiles, int totalFiles,
        CancellationToken cancellationToken)
    {
        if (sourceFolder == null || targetParentFolder == null)
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            string targetFolderName = sourceFolder.Name;
            int suffix = 1;
            
            // Handle duplicate folder names
            while (true)
            {
                try
                {
                    var existingFolder = targetParentFolder.Folders[targetFolderName];
                    if (existingFolder != null)
                    {
                        targetFolderName = $"{sourceFolder.Name} ({suffix++})";
                    }
                }
                catch
                {
                    // Folder doesn't exist, we can use this name
                    break;
                }
            }

            progress.Report((processedFiles * 100.0 / totalFiles, $"Creating folder: {targetFolderName}"));
            var targetFolder = targetParentFolder.Folders.Add(targetFolderName);

            // Copy messages in this folder
            CopyMessages(sourceFolder, targetFolder, progress, processedFiles, totalFiles, cancellationToken);

            // Recursively copy subfolders
            foreach (RDOFolder sourceSubFolder in sourceFolder.Folders)
            {
                cancellationToken.ThrowIfCancellationRequested();
                CopyFolder(sourceSubFolder, targetFolder, progress, processedFiles, totalFiles, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            progress.Report((processedFiles * 100.0 / totalFiles, 
                $"Warning: Error in folder {sourceFolder.Name}: {ex.Message}"));
            throw;
        }
    }

    private void CopyMessages(RDOFolder sourceFolder, RDOFolder targetFolder, 
        IProgress<(double percentage, string message)> progress, int processedFiles, int totalFiles,
        CancellationToken cancellationToken)
    {
        if (sourceFolder == null || targetFolder == null)
        {
            return;
        }

        var items = sourceFolder.Items;
        var totalMessages = items.Count;
        var processedMessages = 0;

        for (int i = 1; i <= totalMessages; i++)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var item = items[i];
                processedMessages++;

                if (!(item is RDOMail message))
                {
                    continue;
                }

                string tempFilePath = Path.GetTempFileName();
                try
                {
                    progress.Report((processedFiles * 100.0 / totalFiles, 
                        $"Processing message {processedMessages}/{totalMessages}: {message.Subject}"));

                    message.SaveAs(tempFilePath, rdoSaveAsType.olMSG);
                    var newMessage = targetFolder.Items.Add("IPM.Note") as RDOMail;
                    if (newMessage != null)
                    {
                        newMessage.Import(tempFilePath, rdoSaveAsType.olMSG);
                        newMessage.Save();
                    }
                }
                finally
                {
                    try
                    {
                        if (File.Exists(tempFilePath))
                        {
                            File.Delete(tempFilePath);
                        }
                    }
                    catch { /* Ignore cleanup errors */ }
                }
            }
            catch (Exception ex)
            {
                progress.Report((processedFiles * 100.0 / totalFiles, 
                    $"Warning: Error processing message {processedMessages}: {ex.Message}"));
                // Continue with next message despite errors
            }
        }
    }
}