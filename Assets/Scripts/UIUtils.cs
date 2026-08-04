using Unity.Properties;
using UnityEngine.UIElements;

public static class UIUtils
{
    public static void BindToLabel(Label label, object source, string propertyPath)
    {
        label.dataSource = source;
        label.SetBinding(nameof(Label.text), new DataBinding
        {
            dataSourcePath = new PropertyPath(propertyPath),
            bindingMode = BindingMode.ToTarget
        });
    }
}