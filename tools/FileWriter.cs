namespace reflection_experiments.tools;

public class FileWriter
{
    private static readonly string s_currentDir = Directory.GetCurrentDirectory();
    
    public void WriteAtRoot(string fileName, string extension, string content)
    {
        if (!extension.StartsWith('.'))
        {
            throw new ArgumentException("Extension format is invalid. Must start with a '.'.");
        }

        string fullPath = Path.Combine(s_currentDir, fileName + extension);

        File.WriteAllText(fullPath, content);
    }
}