﻿using System.IO;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace CatAsset.Runtime
{
        
    /// <summary>
    /// 资源包下载完成回调的原型
    /// </summary>
    public delegate void BundleDownloadedCallback(bool success, BundleManifestInfo info);
    
    /// <summary>
    /// 资源包下载进度更新回调的原型
    /// </summary>
    public delegate void DownloadBundleUpdateCallback(ulong deltaDownloadedBytes,ulong totalDownloadedBytes, BundleManifestInfo info);

    /// <summary>
    /// 资源包下载任务
    /// </summary>
    public class DownloadBundleTask : BaseTask
    {
        private BundleManifestInfo bundleManifestInfo;
        private GroupUpdater groupUpdater;
        private string downloadUri;
        private string localFilePath;
        private BundleDownloadedCallback onBundleDownloadedCallback;
        private DownloadBundleUpdateCallback onDownloadUpdateCallback;
        

        private string localTempFilePath;
        private UnityWebRequestAsyncOperation op;
        
        private ulong downloadedBytes;


        private const int maxRetryCount = 3;
        private int retriedCount;

        /// <inheritdoc />
        public override float Progress
        {
            get
            {
                if (op == null)
                {
                    return 0;
                }
                return op.webRequest.downloadProgress;
            }
        }

        /// <inheritdoc />
        public override void Run()
        {
            if (groupUpdater.State == GroupUpdaterState.Paused)
            {
                //处理下载暂停 暂停只对还未开始执行的下载任务有效
                return;
            }

            //先检查本地是否已存在临时下载文件
            ulong oldFileLength = 0;
            if (File.Exists(localTempFilePath))
            {
                //检查已下载的字节数
                FileInfo fi = new FileInfo(localTempFilePath);
                oldFileLength = (ulong)fi.Length;
            }

            UnityWebRequest uwr = new UnityWebRequest(downloadUri);
            if (oldFileLength > 0)
            {
                //处理断点续传
                uwr.SetRequestHeader("Range", $"bytes={oldFileLength}-");
            }
            uwr.downloadHandler = new DownloadHandlerFile(localTempFilePath, oldFileLength > 0);
            op = uwr.SendWebRequest();
        }

        /// <inheritdoc />
        public override void Update()
        {
            if (op == null)
            {
                //被暂停了
                State = TaskState.Free;
                return;
            }

            if (!op.webRequest.isDone)
            {
                //下载中
                State = TaskState.Running;
                ulong newDownloadedBytes = op.webRequest.downloadedBytes;
                if (downloadedBytes != newDownloadedBytes)
                {
                    ulong deltaDownloadedBytes = newDownloadedBytes - downloadedBytes;
                    onDownloadUpdateCallback?.Invoke(deltaDownloadedBytes,newDownloadedBytes,bundleManifestInfo);
                    downloadedBytes = newDownloadedBytes;
                }
                return;
            }

            if (op.webRequest.result != UnityWebRequest.Result.Success)
            {
                //下载失败 重试
                if (RetryDownload())
                {
                    Debug.Log($"下载失败准备重试：{Name},错误信息：{op.webRequest.error}，当前重试次数：{retriedCount}");
                }
                else
                {
                    //重试次数达到上限 通知失败
                    Debug.LogError($"下载失败重试次数达到上限：{Name},错误信息：{op.webRequest.error}，当前重试次数：{retriedCount}");
                    State = TaskState.Finished;
                    onBundleDownloadedCallback?.Invoke(false ,bundleManifestInfo);
                }
                return;
            }

            //下载成功 开始校验
            //先对比文件长度
            FileInfo fi = new FileInfo(localTempFilePath);
            bool isVerify = fi.Length == bundleManifestInfo.Length;
            if (isVerify)
            {
                //文件长度对得上 再校验MD5
                string md5 = RuntimeUtil.GetFileMD5(localTempFilePath);
                isVerify = md5 == bundleManifestInfo.MD5;
            }

            if (!isVerify)
            {
                //校验失败 删除临时下载文件 尝试重新下载
                File.Delete(localTempFilePath);

                if (RetryDownload())
                {
                    Debug.Log($"校验失败准备重试：{Name}，当前重试次数：{retriedCount}");
                }
                else
                {
                    //重试次数达到上限 通知失败
                    Debug.LogError($"下载失败重试次数达到上限：{Name}，当前重试次数：{retriedCount}");
                    State = TaskState.Finished;
                    onBundleDownloadedCallback?.Invoke(false ,bundleManifestInfo);
                }

                return;
            }


            //校验成功
            State = TaskState.Finished;

            //将临时下载文件覆盖到正式文件上
            if (File.Exists(localFilePath))
            {
                File.Delete(localFilePath);
            }
            File.Move(localTempFilePath, localFilePath);
            onBundleDownloadedCallback?.Invoke(true,bundleManifestInfo);
        }

        /// <summary>
        /// 尝试重新下载
        /// </summary>
        private bool RetryDownload()
        {
            if (retriedCount < maxRetryCount)
            {
                //重试
                retriedCount++;
                State = TaskState.Free;
                return true;
            }

            return false;
        }

        public static DownloadBundleTask Create(TaskRunner owner, string name, BundleManifestInfo bundleManifestInfo,
            GroupUpdater groupUpdater, string downloadUri, string localFilePath,
            BundleDownloadedCallback onBundleDownloadedCallback, DownloadBundleUpdateCallback onDownloadUpdateCallback)
        {
            DownloadBundleTask task = ReferencePool.Get<DownloadBundleTask>();
            task.CreateBase(owner, name);

            task.bundleManifestInfo = bundleManifestInfo;
            task.groupUpdater = groupUpdater;
            task.downloadUri = downloadUri;
            task.localFilePath = localFilePath;
            task.onBundleDownloadedCallback = onBundleDownloadedCallback;
            task.onDownloadUpdateCallback = onDownloadUpdateCallback;
            task.localTempFilePath = localFilePath + ".downloading";

            return task;
        }

        public override void Clear()
        {
            base.Clear();

            bundleManifestInfo = default;
            groupUpdater = default;
            downloadUri = default;
            localFilePath = default;
            onBundleDownloadedCallback = default;

            localTempFilePath = default;
            op = default;
            
            downloadedBytes = default;

            retriedCount = default;
        }
    }
}
