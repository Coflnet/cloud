## Snippets
Snippets are increadable useful, you may use a Command snippet to create a basic command. Add it to the snippet folder.

Mac                 ~/Library/MonoDevelop-{version}/Snippets/
Windows Vista/7     ~/AppData/Roaming/MonoDevelop-{version}/Snippets/
Linux               ~/.local/share/MonoDevelop-{version}/Snippets/

```
<?xml version="1.0" encoding="utf-8"?>
<CodeTemplates version="3.0">
  <CodeTemplate version="2.0">
    <Header>
      <_Group>C#</_Group>
      <Version />
      <MimeType>text/x-csharp</MimeType>
      <Shortcut>command</Shortcut>
      <_Description>Command Template</_Description>
      <TemplateType>Expansion</TemplateType>
    </Header>
    <Variables>
      <Variable name="name" isIdentifier="true">
        <Default>MyCommand</Default>
        <_ToolTip>Unique Command Identifier</_ToolTip>
      </Variable>
      <Variable name="slug">
        <Default>myCommand</Default>
        <_ToolTip>Unique Command Identifier</_ToolTip>
        <Function>GetSimpleTypeName("System#Exception")</Function>
      </Variable>
      <Variable name="optionalPermissions">
        <Default>WritePermission.Instance</Default>
      </Variable>
    </Variables>
    <Code><![CDATA[public class $name$ : Command
{
    /// <summary>
    /// Execute the command logic with specified data.
    /// </summary>
    /// <param name="data"><see cref="MessageData"/> passed over the network .</param>
    public override void Execute(MessageData data)
    {$end$

    }

    /// <summary>
    /// Special settings and Permissions for this <see cref="Command"/>
    /// </summary>
    /// <returns>The settings.</returns>
    protected override CommandSettings GetSettings()
    {
        return new CommandSettings($optionalPermissions$);
    }
    /// <summary>
    /// Gets the globally unique slug (short human readable id).
    /// </summary>
    /// <returns>The slug .</returns>
    public override string Slug
    {
        return "$name$";
    }
}]]></Code>
  </CodeTemplate>
</CodeTemplates>
```
