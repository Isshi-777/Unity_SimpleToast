using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using TMPro;
using UnityEngine;

namespace Isshi777.SimpleToast
{
	/// <summary>
	/// シンプルなToastオブジェクト
	/// </summary>
	public class SimpleToast : MonoBehaviour
	{
		[SerializeField] private TextMeshProUGUI text;
		[SerializeField] private AnimationClip animClip;

		/// <summary>
		/// 表示中であるか
		/// </summary>
		public bool IsDisplaying => this.gameObject.activeSelf;

		/// <summary>
		///　表示
		/// </summary>
		/// <param name="message">メッセージ</param>
		/// <param name="cancelToken">キャンセルトークン</param>
		public async UniTask Display(string message, CancellationToken cancelToken)
		{
			cancelToken.ThrowIfCancellationRequested();

			this.text.text = message;
			this.gameObject.SetActive(true);
			try
			{
				await UniTask.Delay(TimeSpan.FromSeconds(this.animClip.length), cancellationToken: cancelToken);
			}
			finally
			{
				this.gameObject.SetActive(false);
			}
		}
	}
}
