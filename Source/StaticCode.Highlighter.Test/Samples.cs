using Core.Resources;

namespace StaticCode.Highlighter.Test;

public static class Samples
{
    private static readonly ResourceService R = ResourceService.FromType(typeof(Samples));
    public static string CSharp() => R.GetStringByName("StaticCode.Highlighter.Test.Res.Samples.CSharp.cs");
    
}