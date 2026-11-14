using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

/// <summary>
/// System.Type that Unity can serialize.
/// The type is written to data before serialization and read back after deserialization.
/// </summary>
[System.Serializable]
public class SerializableType : ISerializationCallbackReceiver
{
    /// <summary>Full name of the type.</summary>
    public string name;

    /// <summary>The type.</summary>
    [SerializeField] public System.Type type;

    /// <summary>Binary form of the type.</summary>
    public byte[] data;

    /// <summary>Creates a SerializableType for aType.</summary>
    public SerializableType(System.Type aType)
    {
        name = aType.FullName;
        type = aType;
    }

    /// <summary>Reads a type written by Write, including generic arguments.</summary>
    /// <returns>The type, or null for a null type.</returns>
    public static System.Type Read(BinaryReader aReader)
    {
        var paramCount = aReader.ReadByte();
        if (paramCount == 0xFF)
            return null;
        var typeName = aReader.ReadString();
        var type = System.Type.GetType(typeName);
        if (type == null)
            throw new System.Exception("Can't find type; '" + typeName + "'");
        if (type.IsGenericTypeDefinition && paramCount > 0)
        {
            var p = new System.Type[paramCount];
            for (int i = 0; i < paramCount; i++)
            {
                p[i] = Read(aReader);
            }
            type = type.MakeGenericType(p);
        }
        return type;
    }

    /// <summary>Writes aType with its assembly qualified name and its generic arguments.</summary>
    public static void Write(BinaryWriter aWriter, System.Type aType)
    {
        if (aType == null)
        {
            aWriter.Write((byte)0xFF);
            return;
        }
        if (aType.IsGenericType)
        {
            var t = aType.GetGenericTypeDefinition();
            var p = aType.GetGenericArguments();
            aWriter.Write((byte)p.Length);
            aWriter.Write(t.AssemblyQualifiedName);
            for (int i = 0; i < p.Length; i++)
            {
                Write(aWriter, p[i]);
            }
            return;
        }
        aWriter.Write((byte)0);
        aWriter.Write(aType.AssemblyQualifiedName);
    }


    public void OnBeforeSerialize()
    {
        using (var stream = new MemoryStream())
        using (var w = new BinaryWriter(stream))
        {
            Write(w, type);
            data = stream.ToArray();
        }
    }

    public void OnAfterDeserialize()
    {
       
        using (var stream = new MemoryStream(data))
        {
            using (var r = new BinaryReader(stream))
            {
                type = Read(r);
            }
        }
       
    }
}
