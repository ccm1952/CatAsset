﻿using System.Collections.Generic;
using System.IO;

namespace CatAsset.Editor
{
    /// <summary>
    /// 将指定目录下所有一级子目录各自使用NAssetToOneBundle规则进行构建
    /// </summary>
    public class NAssetToOneBundleWithTopDirectory : NAssetToOneBundle
    {
        /// <inheritdoc />
        public override List<BundleBuildInfo> GetBundleList(BundleBuildDirectory bundleBuildDirectory)
        {
            List<BundleBuildInfo> result = new List<BundleBuildInfo>();

            if (Directory.Exists(bundleBuildDirectory.DirectoryName))
            {
                DirectoryInfo dirInfo = new DirectoryInfo(bundleBuildDirectory.DirectoryName);

                //获取所有一级目录
                DirectoryInfo[] topDirectories = dirInfo.GetDirectories("*", SearchOption.TopDirectoryOnly);

                foreach (DirectoryInfo topDirInfo in topDirectories)
                {
                    //每个一级目录构建成一个资源包
                    string assetsDir = EditorUtil.FullNameToAssetName(topDirInfo.FullName);
                    if (EditorUtil.IsChildDirectory(assetsDir,bundleBuildDirectory.DirectoryName))
                    {
                        //跳过子构建目录
                        continue;
                    }
                    BundleBuildInfo info = GetNAssetToOneBundle(assetsDir,bundleBuildDirectory.RuleRegex, bundleBuildDirectory.Group);
                    result.Add(info);
                }
            }

            return result;
        }
    }
}