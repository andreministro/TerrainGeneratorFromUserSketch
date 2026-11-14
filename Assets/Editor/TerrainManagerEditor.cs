using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

[CustomEditor(typeof(TerrainManager))]
[CanEditMultipleObjects]
internal sealed class TerrainManagerEditor : Editor
{
    public override VisualElement CreateInspectorGUI()
    {
        var inspector = new VisualElement();
        using var property = serializedObject.GetIterator();
        for (bool enterChildren = true; property.NextVisible(enterChildren); enterChildren = false)
        {
            if (property.name == "biomeNoise")
            {
                inspector.Add(CreateBiomeNoise(property));
                continue;
            }

            var field = new PropertyField(property.Copy());
            field.SetEnabled(property.name != "m_Script");
            inspector.Add(field);
        }

        return inspector;
    }

    private Foldout CreateBiomeNoise(SerializedProperty noise)
    {
        var foldout = CreateFoldout(noise);
        using var vegetation = noise.FindPropertyRelative("vegetation");
        using var biome = noise.Copy();
        for (bool enterChildren = true; biome.NextVisible(enterChildren) && biome.depth > noise.depth; enterChildren = false)
        {
            if (biome.name != "vegetation")
                foldout.Add(CreateBiome(biome, vegetation));
        }
        return foldout;
    }

    private VisualElement CreateBiome(SerializedProperty biome, SerializedProperty vegetation)
    {
        (string plantPath, string plantLabel) = biome.name switch
        {
            "temperateForest" or "hotDesert" or "tropicalForest" => (biome.name, null),
            "taiga" => ("taigaTrees", "Trees"),
            "coldDesert" => ("coldDesertFrozenShrubs", "Frozen Shrubs"),
            "savannah" => ("savannahTrees", "Trees"),
            _ => (null, null)
        };
        if (plantPath == null)
            return new PropertyField(biome.Copy());

        using var plants = vegetation.FindPropertyRelative(plantPath);
        var biomeFoldout = CreateFoldout(biome);
        AddFields(biomeFoldout, biome);

        var vegetationFoldout = new Foldout
        {
            text = "Vegetation",
            viewDataKey = biome.propertyPath + ".vegetation"
        };
        if (plantLabel == null)
            AddFields(vegetationFoldout, plants);
        else
            vegetationFoldout.Add(new PropertyField(plants.Copy(), plantLabel));
        biomeFoldout.Add(vegetationFoldout);
        return biomeFoldout;
    }

    private static void AddFields(VisualElement container, SerializedProperty group)
    {
        using var child = group.Copy();
        for (bool enterChildren = true; child.NextVisible(enterChildren) && child.depth > group.depth; enterChildren = false)
            container.Add(new PropertyField(child.Copy()));
    }

    private Foldout CreateFoldout(SerializedProperty property)
    {
        string path = property.propertyPath;
        var foldout = new Foldout
        {
            text = property.displayName,
            tooltip = property.tooltip,
            value = property.isExpanded,
            viewDataKey = path
        };
        foldout.RegisterValueChangedCallback(change =>
        {
            if (change.target == foldout)
            {
                using var state = serializedObject.FindProperty(path);
                state.isExpanded = change.newValue;
            }
        });
        return foldout;
    }
}
