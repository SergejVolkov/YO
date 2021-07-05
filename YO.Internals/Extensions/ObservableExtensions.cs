using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DynamicData.Binding;
using ReactiveUI;

namespace YO.Internals.Extensions
{
	public static class ObservableExtensions
	{
		public static IObservable<TProperty> WhereNotNullValues<TObject, TProperty>(
			this IObservable<PropertyValue<TObject, TProperty?>> observable)
			=> observable.Select(p => p.Value)
						 .WhereNotNull();
		
		public static IObservable<TProperty?> WhereNullValues<TObject, TProperty>(
			this IObservable<PropertyValue<TObject, TProperty>> observable)
			=> observable.Select(p => p.Value)
						 .WhereNull();
		
		public static IObservable<T?> WhereNull<T>(this IObservable<T?> observable)
			=> observable.Where(v => v is null);

		public static IObservable<string> WhereNotNullOrEmpty(this IObservable<string?> observable)
			=> observable.WhereNotNull()
						 .Where(s => !string.IsNullOrEmpty(s));

		public static IObservable<string?> WhereNullOrEmpty(this IObservable<string?> observable)
			=> observable.WhereNull()
						 .Where(string.IsNullOrEmpty);

		public static IDisposable SubscribeDiscard<T>(this IObservable<T> observable, Action action)
			=> observable.Subscribe(_ => action());
		
		public static IDisposable SubscribeAsync<T>(this IObservable<T> observable, Func<T, Task> asyncFunc)
			=> observable.Subscribe(async v => await asyncFunc(v));
		
		public static IDisposable SubscribeAsyncDiscard<T>(this IObservable<T> observable, Func<Task> asyncFunc)
			=> observable.Subscribe(async _ => await asyncFunc());
	}
}