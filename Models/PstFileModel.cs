using System;

namespace PSTFuze.Models;

public class PstFileModel
{
    public required string FilePath { get; set; }
    public required string FileName { get; set; }
    public long FileSize { get; set; }
    public DateTime LastModified { get; set; }
    public bool IsCompressed { get; set; }
    public bool IsEncrypted { get; set; }
    public string Status { get; set; } = "Ready";
}