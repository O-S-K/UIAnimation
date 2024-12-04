using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

namespace GameUtil
{
    public class AnimationSequence : MonoBehaviour
    {
        public enum TweenStatus
        {
            None,
            Start
        }

        public bool PlayOnEnable = true;
        public UpdateType UpdateType = UpdateType.Normal;
        public bool IgnoreTimeScale = true;
        public SingleTween[] AnimationSteps = new SingleTween[0];

        public TweenStatus Status { get; private set; } = TweenStatus.None;

        private Sequence mSequence;

        private void Awake()
        {
            foreach (var tween in AnimationSteps)
            {
                tween.Bind(gameObject);
            }
        }

        private void OnEnable()
        {
            if (PlayOnEnable)
                DoStartTween(null);
        }

        public void DoStartTween(Action action)
        {
            RecoverStatus();

            Status = TweenStatus.Start;
            mSequence?.Kill(); // Clear any existing sequence
            mSequence = DOTween.Sequence().SetUpdate(UpdateType, IgnoreTimeScale);

            float lastTweenInsertTime = 0;

            foreach (var tween in AnimationSteps)
            {
                switch (tween.AddItemType)
                {
                    case SingleTween.ItemType.Tweener:
                        if (!tween.IsValid) continue;

                        switch (tween.ItemLinkType)
                        {
                            case SingleTween.LinkType.Append:
                                lastTweenInsertTime = mSequence.Duration(false);
                                if (tween.OverrideStartStatus)
                                    mSequence.AppendCallback(tween.SetStartStatus);

                                mSequence.Append(tween.BuildTween());
                                break;

                            case SingleTween.LinkType.Join:
                                if (tween.OverrideStartStatus)
                                    mSequence.InsertCallback(lastTweenInsertTime, tween.SetStartStatus);

                                mSequence.Join(tween.BuildTween());
                                break;

                            case SingleTween.LinkType.Insert:
                                lastTweenInsertTime = tween.AtPosition;
                                if (tween.OverrideStartStatus)
                                    mSequence.InsertCallback(lastTweenInsertTime, tween.SetStartStatus);

                                mSequence.Insert(lastTweenInsertTime, tween.BuildTween());
                                break;

                            default:
                                Debug.LogError("Unknown LinkType: " + tween.ItemLinkType);
                                break;
                        }

                        break;

                    case SingleTween.ItemType.Delay:
                        if (tween.Duration > 0)
                            mSequence.AppendInterval(tween.Duration);
                        break;

                    case SingleTween.ItemType.Callback:
                        switch (tween.ItemLinkType)
                        {
                            case SingleTween.LinkType.Append:
                                mSequence.AppendCallback(tween.InvokeCallback);
                                break;

                            case SingleTween.LinkType.Join:
                                mSequence.InsertCallback(lastTweenInsertTime, tween.InvokeCallback);
                                break;

                            case SingleTween.LinkType.Insert:
                                mSequence.InsertCallback(tween.AtPosition, tween.InvokeCallback);
                                break;

                            default:
                                Debug.LogError("Unknown LinkType: " + tween.ItemLinkType);
                                break;
                        }

                        break;

                    default:
                        Debug.LogError("Unknown ItemType: " + tween.AddItemType);
                        break;
                }
            }

            // Complete callback for the entire sequence
            mSequence.OnComplete(() =>
            {
                mSequence = null;
                Status = TweenStatus.None;
                action?.Invoke();
            });
        }

        public float GetDuration()
        {
            float duration = 0;
            foreach (var tween in AnimationSteps)
            {
                if (tween.AddItemType == SingleTween.ItemType.Tweener)
                {
                    duration += tween.Duration;
                }
            }

            return duration;
        }

        public void Preview(float time)
        {
            RecoverStatus();

            foreach (var tween in AnimationSteps)
            {
                if (tween.AddItemType == SingleTween.ItemType.Tweener)
                {
                    //tween.Preview(time);
                }
            }
        }

        private void RecoverStatus()
        {
            foreach (var tween in AnimationSteps)
            {
                tween.RecoverStatus();
            }
        }
    }
}