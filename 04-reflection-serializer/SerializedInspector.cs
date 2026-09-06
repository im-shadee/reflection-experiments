using System.Diagnostics;
using System.Reflection;
using System.Text;
using ReflectionExperiments.Serializer;

public class SerializedInspector
{
    private Type? m_currentType;
    private object? m_currentObject;
    
    private static readonly BindingFlags s_flags = BindingFlags.NonPublic
                                   | BindingFlags.Public
                                   | BindingFlags.Static
                                   | BindingFlags.Instance;
    
    private static readonly eOptions[] s_options = Enum.GetValues<eOptions>(); 

    private enum eOptions
    {
        DisplayInfo = 0,
        SetField,
        InvokeMethod,
        Cancel,
        Deserialize,
    }

    public void StartApp()
    {
        while (true)
        {
            // Shade: Clear the console each application start
            Console.Clear();
            
            // Shade: Loop until a valid class name is entered or 'Exit' is chosen
            while (true)
            {
                Console.WriteLine("Enter a class to inspect (or type 'Exit' to quit).");

                try
                {
                    string? input = Console.ReadLine();

                    // Shade: Failing conditions: input is null/blank/not a valid type
                    if (string.IsNullOrWhiteSpace(input))
                    {
                        ProcessInvalidInput(2000);
                        continue;
                    }
                
                    if (input.Equals("Exit", StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }

                    m_currentType = Type.GetType(input);
                    if (m_currentType == null)
                    {
                        ProcessInvalidInput(2000, $"'{input}' is not a valid class. Please retry.");
                        continue;
                    }
                    
                    break; // Shade: Exit if the inner input-reading loop successfully completed
                }
                catch
                {
                    ProcessInvalidInput(2000);
                    return;
                }
            }
            
            // Shade: Instantiate object and enter options loop
            CreateReflectedInstance();
            ChooseOptions(); // Shade: This will block until the user selects "Cancel / Change Class"
        }
    }

    private void CreateReflectedInstance()
    {
        if (m_currentType == null) return;
        m_currentObject = Activator.CreateInstance(m_currentType);
    }

    private void ChooseOptions()
    {
        bool keepRunning = true;

        while (keepRunning)
        {
            int inputInt = GetMenuSelection(); // Shade: Get and validate user input (1-4)
            eOptions option = (eOptions)(inputInt - 1);

            switch (option)
            {
                case eOptions.DisplayInfo:
                    m_currentType?.PrintInfo(showClassInterfaces: true, showParent: false);
                    break;
            
                case eOptions.SetField:
                    SetField(); 
                    break;
            
                case eOptions.InvokeMethod:
                    InvokeMethod();
                    break;
                
                case eOptions.Deserialize:
                    Deserialize();
                    break;
            
                case eOptions.Cancel:
                default:
                    keepRunning = false;
                    break;
            }
        }

        // Shade: Return back to StartApp() naturally
    }
    
    private int GetMenuSelection()
    {
        int totalOptions = s_options.Length;

        while (true)
        {
            Console.WriteLine("\nChoose your option:");
            Console.WriteLine("\t1. Display type info");
            Console.WriteLine("\t2. Set a field");
            Console.WriteLine("\t3. Invoke a method");
            Console.WriteLine("\t4. Change class");
            Console.WriteLine("\t5. (NEW) Deserialize from file");

            string? input = Console.ReadLine();

            if (!int.TryParse(input, out int selection) || selection < 1 || selection > totalOptions)
            {
                ProcessInvalidInput(2000, $"Please enter a number between 1 and {totalOptions}.");
                continue;
            }

            return selection;
        }
    }

    private void Deserialize()
    {
        FileWriter fileWriter = new FileWriter();
        string json = string.Empty;
        
        try
        {
            json = fileWriter.GetFileContent($"inspector_data_{m_currentType?.Name.ToLower() ?? "null"}", ".meta");
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine("Could not find inspector data file. Filename is either wrong, or no data exists yet.");
            return;
        }
        
        if (m_currentType == null) return;
        
        MethodInfo? method = typeof(Serializer).GetMethod(nameof(Serializer.DeserializeFromJson));
        MethodInfo? genericMethod = method?.MakeGenericMethod(m_currentType);
        
        object? deserializedObj = genericMethod?.Invoke(null, new object[] { json, "m_currentObject" });
        
        if (deserializedObj != null) m_currentObject = deserializedObj;
        Console.WriteLine($"Deserialized object data: " + m_currentObject);
    }
    
    private void SetField()
    {
        FieldInfo? field;
        object value;
        
        while (true)
        {
            Console.WriteLine("Input a field to set. Type 'Help' to display the fields.");
            
            try
            {
                string? input = Console.ReadLine();
                
                if (string.IsNullOrWhiteSpace(input))
                {
                    ProcessInvalidInput(2000);
                    continue;
                }
                
                if (input.Equals("Help", StringComparison.OrdinalIgnoreCase))
                {
                    if (m_currentType != null) ReflectionExtensions.PrintFields(m_currentType);
                    continue;
                }

                field = m_currentType?.GetField(input, s_flags);
                if (field == null)
                {
                    ProcessInvalidInput(2000);
                    continue;
                }
                
                break; // Shade: Exit loop on success
            }
            catch
            {
                ProcessInvalidInput(2000);
                return;
            }
        }
        
        Console.WriteLine($"Current value: {field.GetValue(m_currentObject)}");
        
        while (true)
        {
            Console.WriteLine("Input a value to assign to the field.");
            
            try
            {
                string? input = Console.ReadLine();
                
                if (string.IsNullOrWhiteSpace(input))
                {
                    ProcessInvalidInput(2000);
                    continue;
                }

                // Shade: Try converting the input into the targeted field's type.
                // e.g., if the field is an int and the user inputs "12", the input will be cast into the correct type
                object? valueObj = ReflectionTools.ConvertTo(field.FieldType, input);
                if (valueObj == null)
                {
                    ProcessInvalidInput(2000, $"Cast to type {field.FieldType} failed. Please retry.");
                    continue;
                }
                
                value = valueObj;
                break; // Shade: Exit loop on success
            }
            catch
            {
                ProcessInvalidInput(2000);
                return;
            }
        }

        try
        {
            field?.SetValue(m_currentObject, value);
        }
        catch
        {
            ProcessInvalidInput(2000, "An unknown error occured. Check logs.");
        }
        
        Console.WriteLine($"New value: {field?.GetValue(m_currentObject)}");

        if (m_currentObject != null)
        {
            // Shade: NEW: Serialize the new values into a json file
            using MemoryStream stream = new MemoryStream();
            Serializer.SerializeToJson(m_currentObject, stream);
            
            // Shade: Get the value written in the stream and write it to file
            string json = Encoding.UTF8.GetString(stream.ToArray());
            
            FileWriter fileWriter = new FileWriter();
            fileWriter.WriteAtRoot($"inspector_data_{m_currentType?.Name.ToLower() ?? "null"}", ".meta", json);
        }
    }

    private void InvokeMethod()
    {
        MethodInfo? method;
        
        while (true)
        {
            Console.WriteLine("Input a method to invoke. Type 'Help' to display the methods.");
            
            try
            {
                string? input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input))
                {
                    ProcessInvalidInput(2000);
                    continue;
                }
                
                if (input.Equals("Help", StringComparison.OrdinalIgnoreCase))
                {
                    if (m_currentType != null) ReflectionExtensions.PrintMethods(m_currentType);
                    continue;
                }

                method = m_currentType?.GetMethod(input, s_flags);
                if (method == null)
                {
                    ProcessInvalidInput(2000);
                    continue;
                }
                
                break; // Shade: Exit loop on success
            }
            catch
            {
                ProcessInvalidInput(2000);
                return;
            }
        }

        ParameterInfo[] parameters = method.GetParameters();
        int parametersLength = parameters.Length;

        // Shade: If no parameters are required, invoke the method without passing anything
        if (parametersLength == 0)
        {
            method.Invoke(m_currentObject, null);
            return;
        }
        
        int index = 0;
        List<object?> passedParameters = new List<object?>(parametersLength);

        while (index < parametersLength)
        {
            Type paramType = parameters[index].ParameterType;
            Console.WriteLine($"Input each parameter ({index + 1}/{parametersLength}). Current parameter: {parameters[index].Name}: {paramType}");
            
            try
            {
                string? input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input))
                {
                    ProcessInvalidInput(2000);
                    return;
                }
                
                // Shade: Cast the input to the method parameter's type
                object? valueObj = ReflectionTools.ConvertTo(paramType, input);
                if (valueObj == null)
                {
                    ProcessInvalidInput(2000);
                    return;
                }
                
                // Shade: If the cast was successful, add the cast object to the list of parameters to pass to the
                // Invoke method. Else, prompts the user to retry inputting
                passedParameters.Add(valueObj);
                
                // Shade: Increase the index and go to the next parameter if successful
                index++;
            }
            catch
            {
                ProcessInvalidInput(2000);
                return;
            }
        }
        
        // Shade: Ensure the list of parameters to pass is as long as the list of parameters that the method possesses
        if (passedParameters.Count != parametersLength)
        {
            ProcessInvalidInput(2000, $"Number of parameters passed ({passedParameters.Count}) is higher or " +
                $"lower than the numbers of parameters in methode {method.Name}.");
            return;
        }

        method.Invoke(m_currentObject, passedParameters.ToArray());
    }

    private void ProcessInvalidInput(int msTimeout, string? customMessage = null)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        
        // Shade: Displays the default message if customMessage else customMessage
        Console.WriteLine(customMessage ?? "Invalid input. Please retry.");
        Console.ResetColor();
        
        Thread.Sleep(msTimeout);
    }
}
