﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using SharpYaml.Serialization;
using SiliconStudio.Assets;
using SiliconStudio.Assets.Compiler;
using SiliconStudio.Assets.Diff;
using SiliconStudio.Core;
using SiliconStudio.Core.Annotations;
using SiliconStudio.Core.Diagnostics;
using SiliconStudio.Core.Serialization;
using SiliconStudio.Core.Yaml;
using SiliconStudio.Xenko.Rendering;

namespace SiliconStudio.Xenko.Assets.Model
{
    [DataContract("Model")]
    [AssetDescription(FileExtension, false)]
    [AssetCompiler(typeof(ModelAssetCompiler))]
    [Display(190, "Model", "A 3D model")]
    [AssetFormatVersion(2)]
    [AssetUpgrader(0, 1, 2, typeof(Upgrader))]
    public sealed class ModelAsset : AssetImportTracked, IModelAsset, IAssetCompileTimeDependencies
    {
        /// <summary>
        /// The default file extension used by the <see cref="ModelAsset"/>.
        /// </summary>
        public const string FileExtension = ".xkm3d;pdxm3d";

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelAsset"/> class.
        /// </summary>
        public ModelAsset()
        {
            ScaleImport = 1.0f;
            Materials = new List<ModelMaterial>();
            SetDefaults();
        }

        /// <summary>
        /// Gets or sets the scale import.
        /// </summary>
        /// <value>The scale import.</value>
        /// <userdoc>The scale applied when importing a model.</userdoc>
        [DataMember(10)]
        [DefaultValue(1.0f)]
        public float ScaleImport { get; set; }

        /// <summary>
        /// The materials.
        /// </summary>
        /// <userdoc>
        /// The list of materials in the model.
        /// </userdoc>
        [DataMember(40)]
        [MemberCollection(ReadOnly = true)]
        public List<ModelMaterial> Materials { get; private set; }

        /// <summary>
        /// Gets or sets the Skeleton.
        /// </summary>
        /// <userdoc>
        /// Describes the node hierarchy that will be active at runtime.
        /// </userdoc>
        [DataMember(50)]
        public Skeleton Skeleton { get; set; }

        protected override int InternalBuildOrder
        {
            get { return -100; } // We want Model to be scheduled early since they tend to take the longest (bad concurrency at end of build)
        }

        /// <inheritdoc/>
        [DataMemberIgnore]
        public IEnumerable<KeyValuePair<string, MaterialInstance>> MaterialInstances { get { return Materials.Select(x => new KeyValuePair<string, MaterialInstance>(x.Name, x.MaterialInstance)); } }

        /// <inheritdoc/>
        public IEnumerable<IContentReference> EnumerateCompileTimeDependencies()
        {
            if (Skeleton != null)
            {
                var reference = AttachedReferenceManager.GetAttachedReference(Skeleton);
                if (reference != null)
                {
                    yield return new AssetReference<Asset>(reference.Id, reference.Url);
                }
            }
        }

        class Upgrader : AssetUpgraderBase
        {
            protected override void UpgradeAsset(AssetMigrationContext context, int currentVersion, int targetVersion, dynamic asset, PackageLoadingAssetFile assetFile)
            {
                foreach (var modelMaterial in asset.Materials)
                {
                    var material = modelMaterial.Material;
                    if (material != null)
                    {
                        modelMaterial.MaterialInstance = new YamlMappingNode();
                        modelMaterial.MaterialInstance.Material = material;
                        modelMaterial.Material = DynamicYamlEmpty.Default;
                    }
                }
            }
        }
    }
}