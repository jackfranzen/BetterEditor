using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class TestClass
{
    public bool use = false;
    public Color color = Color.cyan;
    public string stringVar = "default";
    public TestClass(bool useIn, Color colorIn, string stringVarIn)
    {
        use = useIn;
        color = colorIn;
        stringVar = stringVarIn;
    }
    public TestClass() { }

    public TestClass Copy()
    {
        return new TestClass(use, color, stringVar);
    }
}

public class DiscordTest : MonoBehaviour
{
    // -- Hidden Constant Class
    private static readonly TestClass staticTest = new (true, Color.green, "Static");
    
    // -- Serialized Class
    public TestClass test2 = new();

    public static HashSet<TestClass> staticList = new();
    
    public List<TestClass> listVisualize;
    

    // -- Awake: Called when the game starts
    public void Awake()
    {
        staticList.Add(staticTest);
        staticList.Add(test2.Copy());
        
        listVisualize.AddRange(staticList);
    }
}