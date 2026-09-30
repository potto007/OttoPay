using System.Reflection;
using HarmonyLib;

namespace OttoPay.Tests;

/// Every Harmony patch in the mod names a game method that exists, and every parameter it
/// asks for by name is one that method has. Harmony only checks either when the game loads the
/// mod, so a stale target otherwise fails in front of a player. Harmony's own AccessTools
/// cannot start outside the game, so the targets are looked up with plain reflection.
public class PatchTargetTests
{
    private const BindingFlags AnyStatic = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
    private const BindingFlags AnyMember = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
    private static readonly string[] Injected = { "__instance", "__result", "__state", "__runOriginal", "__exception" };

    public static IEnumerable<object[]> Patches()
    {
        foreach (Type type in PatchHolders())
        {
            foreach (MethodInfo method in type.GetMethods(AnyStatic))
            {
                if (method.GetCustomAttributes<HarmonyPatch>().Any())
                    yield return new object[] { $"{type.Name}.{method.Name}" };
            }
        }
    }

    [Fact]
    public void The_mod_has_patches_to_check()
    {
        Assert.True(Patches().Count() >= 12);
    }

    [Theory]
    [MemberData(nameof(Patches))]
    public void The_patch_target_resolves(string patch)
    {
        string[] parts = patch.Split('.');
        MethodInfo method = PatchHolders().Single(type => type.Name == parts[0]).GetMethod(parts[1], AnyStatic)!;
        MethodBase target = Target(patch, method);

        foreach (ParameterInfo parameter in method.GetParameters())
        {
            string name = parameter.Name!;
            if (Injected.Contains(name))
                continue;
            if (name.StartsWith("___"))
            {
                Assert.NotNull(target.DeclaringType!.GetField(name.Substring(3), AnyMember));
                continue;
            }

            Assert.Contains(name, target.GetParameters().Select(original => original.Name));
        }
    }

    // ServerSync is merged into the mod DLL and brings patches of its own, which are its
    // business, so only the mod's namespace is checked.
    private static IEnumerable<Type> PatchHolders()
    {
        return typeof(OttoPayApi).Assembly.GetTypes()
            .Where(type => type.Namespace?.StartsWith("OttoPay") == true && type.GetCustomAttributes<HarmonyPatch>().Any());
    }

    private static MethodBase Target(string patch, MethodInfo method)
    {
        Type? declaringType = null;
        string? methodName = null;
        Type[]? argumentTypes = null;
        foreach (HarmonyPatch attribute in method.GetCustomAttributes<HarmonyPatch>())
        {
            declaringType ??= attribute.info.declaringType;
            methodName ??= attribute.info.methodName;
            argumentTypes ??= attribute.info.argumentTypes;
        }

        Assert.True(declaringType != null && methodName != null, $"{patch} does not name its target.");
        MethodInfo[] candidates = declaringType!.GetMethods(AnyMember).Where(candidate => candidate.Name == methodName).ToArray();
        if (argumentTypes != null)
            candidates = candidates.Where(candidate => candidate.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(argumentTypes)).ToArray();

        Assert.True(candidates.Length == 1, $"{patch} targets {declaringType.Name}.{methodName}, which matches {candidates.Length} methods.");
        return candidates[0];
    }
}
