#nullable enable
using System;
using System.IO;
using PowerSimBuilder.Physics;
using Xunit;

namespace PowerSimBuilder.Tests;

public class ModelYamlTests
{
    private static string WriteTempYaml(string content)
    {
        var p = Path.Combine(Path.GetTempPath(),
            "psb_yaml_" + Guid.NewGuid().ToString("N") + ".yaml");
        File.WriteAllText(p, content);
        return p;
    }

    [Fact]
    public void ReadPortMapping_MissingFile_ReturnsIdentity()
    {
        var m = ModelYaml.ReadPortMapping(
            Path.Combine(Path.GetTempPath(), "psb_nope_" + Guid.NewGuid().ToString("N") + ".yaml"));
        Assert.Empty(m.Inputs);
        Assert.Empty(m.Outputs);
        Assert.Equal("anything", m.ComponentPortFor("anything"));
    }

    [Fact]
    public void ReadPortMapping_RealYaml_ParsesMappings()
    {
        const string yaml =
            "name: PMSM\n" +
            "source:\n" +
            "  type: fmu\n" +
            "  file: source/model.fmu\n" +
            "interface:\n" +
            "  fmi_type: CoSimulation\n" +
            "  inputs:\n" +
            "    u_d: u_d_input\n" +
            "    u_q: u_q_input\n" +
            "  outputs:\n" +
            "    i_d: i_d_output\n" +
            "  parameters: {}\n";
        var path = WriteTempYaml(yaml);
        try
        {
            var m = ModelYaml.ReadPortMapping(path);

            Assert.Equal("u_d_input", m.ComponentPortFor("u_d"));
            Assert.Equal("u_q_input", m.ComponentPortFor("u_q"));
            Assert.Equal("i_d_output", m.ComponentOutputFor("i_d"));
            // Missing mapping -> identity
            Assert.Equal("other", m.ComponentPortFor("other"));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void ReadPortMapping_EmptyValues_FallbackToKey()
    {
        const string yaml =
            "interface:\n" +
            "  inputs:\n" +
            "    a:\n" +
            "    b: \"\"\n";
        var path = WriteTempYaml(yaml);
        try
        {
            var m = ModelYaml.ReadPortMapping(path);
            Assert.Equal("a", m.ComponentPortFor("a"));
            Assert.Equal("b", m.ComponentPortFor("b"));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void ReadPortMapping_QuotedKeysAndValues_AreUnquoted()
    {
        const string yaml =
            "interface:\n" +
            "  inputs:\n" +
            "    \"p_in\": \"P_in\"\n";
        var path = WriteTempYaml(yaml);
        try
        {
            var m = ModelYaml.ReadPortMapping(path);
            Assert.Equal("P_in", m.ComponentPortFor("p_in"));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void ReadPortMapping_BrokenYaml_ReturnsIdentity()
    {
        var path = WriteTempYaml("::: not yaml :::\n");
        try
        {
            var m = ModelYaml.ReadPortMapping(path);
            // Should not throw; returns identity
            Assert.NotNull(m);
        }
        finally { File.Delete(path); }
    }
}