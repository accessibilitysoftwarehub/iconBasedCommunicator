﻿using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows;
using OptiKey.Enums;
using OptiKey.Extensions;
using OptiKey.Models;
using OptiKey.Services;

namespace OptiKey.Observables.PointSources
{
    public class PointServiceSource : IPointSource
    {
        #region Fields
        
        private readonly TimeSpan pointTtl;
        private readonly IPointService pointGeneratingService;

        private IObservable<Timestamped<PointAndKeyValue?>> sequence;

        #endregion

        #region Ctor

        public PointServiceSource(
            TimeSpan pointTtl,
            IPointService pointGeneratingService)
        {
            this.pointTtl = pointTtl;
            this.pointGeneratingService = pointGeneratingService;
        }

        #endregion

        #region Properties

        public RunningStates State { get; set; }

        public Dictionary<Rect, KeyValue> PointToKeyValueMap { private get; set; }

        public IObservable<Timestamped<PointAndKeyValue?>> Sequence
        {
            get
            {
                if (sequence == null)
                {
                    sequence = Observable.FromEventPattern<Timestamped<Point>>(
                            eh => pointGeneratingService.Point += eh,
                            eh => pointGeneratingService.Point -= eh)
                        .Where(_ => State == RunningStates.Running)
                        .Select(ep => ep.EventArgs)
                        .PublishLivePointsOnly(pointTtl)
                        .Select(tp => new Timestamped<PointAndKeyValue?>(tp.Value.ToPointAndKeyValue(PointToKeyValueMap), tp.Timestamp))
                        .Replay(1) //Buffer one value for every subscriber so there is always a 'most recent' point available
                        .RefCount();
                }

                return sequence;
            }
        }
        
        #endregion
    }
}
