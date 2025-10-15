using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Isshi777.SimpleToast
{
    public class SimpleToastDemo : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Button button1;
        [SerializeField] private Button button2;
        [SerializeField] private SimpleToast toastPrefab;
        [SerializeField] private RectTransform toastParent;

        [Header("Pool Settings")]
        [Min(0)]
        [SerializeField] private int poolSize = 3;

        private readonly List<SimpleToast> pooledToastList = new();
        private readonly Dictionary<string, CancellationTokenSource> activeToastCtsDic = new();

        private void Start()
        {
            for (int i = 0; i < this.poolSize; i++)
            {
                var toast = Instantiate(this.toastPrefab, this.toastParent);
                toast.gameObject.SetActive(false);
                this.pooledToastList.Add(toast);
            }

            this.button1.OnClickAsObservable()
                .Subscribe(_ => this.DisplayToast("Simple Toast Message !", "button1"))
                .AddTo(this);
            this.button2.OnClickAsObservable()
                .Subscribe(_ => this.DisplayToast("Another Toast Message !!", "button2"))
                .AddTo(this);
        }

        private void DisplayToast(string message, string key)
        {
            this.DisplayToast(message, key, this.destroyCancellationToken).Forget();
        }

        private async UniTask DisplayToast(string message, string key, CancellationToken cancelToken)
        {
            if (this.activeToastCtsDic.TryGetValue(key, out var activeToastCts))
            {
                // ここではDisposeしない（進行中のコールバックが触っている可能性があるため）
                activeToastCts.Cancel();
            }

            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancelToken);
            this.activeToastCtsDic[key] = cts;

            var (toast, isTemporary) = this.GetToast();
#if UNITY_EDITOR
            toast.gameObject.name = $"Toast [{key}]";
#endif
            try
            {
                // キャンセル時の例外を抑制
                await toast.Display(message, cts.Token);
            }
            finally
            {
                if (this.activeToastCtsDic.TryGetValue(key, out var resultCts) && resultCts == cts)
                {
                    this.activeToastCtsDic.Remove(key);
                }
                cts.Dispose();

                if (isTemporary)
                {
                    Destroy(toast.gameObject);
                }
            }
        }

        private (SimpleToast toast, bool isTemporary) GetToast()
        {
            bool isTemporary = false;
            SimpleToast toast = this.pooledToastList.Find(t => !t.IsDisplaying);
            if (toast == null)
            {
                var newToast = Instantiate(this.toastPrefab, this.toastParent);
                var hasPoolSpace = this.pooledToastList.Count < this.poolSize;
                if (hasPoolSpace)
                {
                    this.pooledToastList.Add(newToast);
                    toast = newToast;
                }
                else
                {
                    toast = newToast;
                    isTemporary = true;
                }
            }

            toast.transform.SetAsLastSibling();
            return (toast, isTemporary);
        }

        private void OnDestroy()
        {
            // 念のための一括キャンセル・解放
            foreach (var kv in this.activeToastCtsDic)
            {
	            try
	            {
		            kv.Value.Cancel();
		            kv.Value.Dispose();
	            }
	            catch
	            {
	            }
            }
            this.activeToastCtsDic.Clear();
        }
    }
}
