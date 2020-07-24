using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Collections.Concurrent;
using System.Threading;

namespace Fritz.Chatbot.Helpers
{
    public static class ObservableExtensions
    {
        /// <summary>
        /// Pass through up to <paramref name="count"/> items downstream within given <paramref name="interval"/>.
        /// Once more elements are about to get through they will become buffered, until interval resets.
        /// </summary>
        public static IObservable<T> Throttle<T>(this IObservable<T> source, int count, TimeSpan interval) => 
            new Throttle<T>(source, count, interval);
    } 
    
    /// <summary>
    /// Custom Throttle implementation, because the default provided with rx.net does not work like we want.
    /// code is by @Horusiath Who confirmed that i was indeed not crazy, and that rx does not has an operator OOB 
    /// that behaves like this.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Throttle<T> : IObservable<T>
    {
        private readonly IObservable<T> _source;
        private readonly int _count;
        private readonly TimeSpan _interval;

        public Throttle(IObservable<T> source, int count, TimeSpan interval)
        {
            _source = source;
            _count = count;
            _interval = interval;
        }

        public IDisposable Subscribe(IObserver<T> observer) => 
            _source.SubscribeSafe(new Observer(observer, _count, _interval));

        private sealed class Observer : IObserver<T>
        {
            private readonly IObserver<T> _observer;
            private readonly int _count;
            private readonly Timer _timer;
            private readonly ConcurrentQueue<T> _buffer;
            private int _remaining;
            
            public Observer(IObserver<T> observer, int count, TimeSpan interval)
            {
                _observer = observer;
                _remaining = _count = count;
                _buffer = new ConcurrentQueue<T>();
                _timer = new Timer(_ =>
                {
                    // first, try to dequeue up to `_count` buffered items
                    // after that is done, reset `_remaining` quota to what's left
                    var i = _count;
                    while (i > 0 && _buffer.TryDequeue(out var value))
                    {
                        i--;
                        _observer.OnNext(value);
                    }

                    // reset remaining count at the end of the interval
                    Interlocked.Exchange(ref _remaining, i);
                }, null, interval, interval);
            }

            public void OnCompleted()
            {
                // what to do with buffered items? Up to you.
                _timer.Dispose();
                _observer.OnCompleted();
            }

            public void OnError(Exception error)
            {
                _observer.OnError(error);
            }

            public void OnNext(T value)
            {
                if (Interlocked.Decrement(ref _remaining) >= 0)
                {
                    // if we have free quota to spare in this interval, emit value downstream
                    _observer.OnNext(value);
                }
                else
                {
                    // otherwise buffer value until timer will reset it
                    _buffer.Enqueue(value);
                }
            }
        }
    }
}