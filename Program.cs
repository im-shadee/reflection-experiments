using System.Diagnostics;

public class Program
{
    private enum ePrograms
    {
        Inspector = 0,
        SerializedInspector,
        LaunchTests,
    }
    
    private static readonly ePrograms[] s_options = Enum.GetValues<ePrograms>(); 
    
    private static void StartInspector()
    {
        InspectorApp app = new InspectorApp();
        app.StartApp();
    }

    private static void StartSerializedInspector()
    {
        SerializedInspector inspector = new SerializedInspector();
        inspector.StartApp();
    }

    private static void StartTests()
    {
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "test",
            UseShellExecute = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            EnvironmentVariables =
            {
                ["LOG_DEBUG"] = "true"
            }
        };

        try
        {
            using Process? process = Process.Start(startInfo);
            process?.WaitForExit();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to start dotnet test: {ex.Message}");
        }
    }
    
    public static void Main(string[] args)
    {
        int totalOptions = s_options.Length;
        int selectedOption;
        
        Console.WriteLine($"What do you want to do?");
        Console.WriteLine("\t1. Start the reflection inspector");
        Console.WriteLine("\t2. Start the reflection inspector with serialization");
        Console.WriteLine("\t3. Run tests");
        Console.WriteLine();
        
        while (true)
        {
            try
            {
                string? input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input))
                {
                    Console.WriteLine($"Input is empty. Please retry.");
                    continue;
                }
                
                if (!int.TryParse(input, out selectedOption) || selectedOption < 1 || selectedOption > totalOptions)
                {
                    Console.Write($"Please enter a number between 1 and {totalOptions}.");
                }
                else
                {
                    break;
                }
            }
            catch
            {
                Console.WriteLine($"An error occured. Program will close.");
            }
        }
        
        ePrograms program = (ePrograms)(selectedOption - 1);

        switch (program)
        {
            case ePrograms.Inspector:
                StartInspector();
                break;
            
            case ePrograms.SerializedInspector:
                StartSerializedInspector();
                break;
            
            case ePrograms.LaunchTests:
                StartTests();
                break;
        }
    }
}
