// (c) Copyright ESRI.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using ESRI.ArcGIS.Runtime.Xaml;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace ESRI.ArcGIS.Runtime.Toolkit.Xaml
{
	/// <summary>
	/// The Attribution Control displays Copyright information for Layers that have the IAttribution 
	/// Interface implemented.
	/// </summary>
	public class Attribution : Control
	{
		#region Constructor

		/// <summary>
		/// Initializes a new instance of the <see cref="Attribution"/> class.
		/// </summary>
		public Attribution()
		{
			DefaultStyleKey = typeof(Attribution);
		}
		#endregion

		#region DependencyProperty Map


		/// <summary>
		/// Gets or sets the map whose layers it should display attribution for.
		/// </summary>
		public Map Map
		{
			get { return GetValue(MapProperty) as Map; }
			set { SetValue(MapProperty, value); }
		}

		/// <summary>
		/// Identifies the <see cref="Map"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty MapProperty =
			DependencyProperty.Register("Map", typeof(Map), typeof(Attribution), new PropertyMetadata(null, OnMapPropertyChanged));

		private static void OnMapPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is Attribution)
				(d as Attribution).OnMapPropertyChanged(e.OldValue as Map, e.NewValue as Map);
		}

		private void OnMapPropertyChanged(Map oldMap, Map newMap)
		{
			if (oldMap != null)
				DetachLayersHandler(oldMap.Layers);
			if (newMap != null)
				AttachLayersHandler(newMap.Layers);
			UpdateAttributionItems();
		}
		#endregion

		#region Items Dependency Property

		/// <summary>
		/// Gets the items to display in attribution control.
		/// </summary>
		/// <value>
		/// The items.
		/// </value>
		public IEnumerable<string> Items
		{
			get { return (IEnumerable<string>)GetValue(ItemsProperty); }
			internal set { SetValue(ItemsProperty, value); }
		}

		/// <summary>
		/// Identifies the <see cref="Items"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty ItemsProperty =
			DependencyProperty.Register("Items", typeof(IEnumerable<string>), typeof(Attribution), new PropertyMetadata(null));
		
		#endregion

		#region Map Event Handlers

		private void DetachLayersHandler(IEnumerable<Layer> layers)
		{
			if (layers != null)
			{
				if(layers is INotifyCollectionChanged)
					(layers as INotifyCollectionChanged).CollectionChanged -= Layers_CollectionChanged;
				foreach (Layer layer in layers)
					DetachLayerHandler(layer);
			}
		}

		private void AttachLayersHandler(IEnumerable<Layer> layers)
		{
			if (layers != null)
			{
				if (layers is INotifyCollectionChanged)
					(layers as INotifyCollectionChanged).CollectionChanged += Layers_CollectionChanged;
				foreach (Layer layer in layers)
					AttachLayerHandler(layer);
			}
		}

		private void AttachLayerHandler(Layer layer)
		{
			layer.PropertyChanged += Layer_PropertyChanged;
		}

		private void DetachLayerHandler(Layer layer)
		{
			layer.PropertyChanged -= Layer_PropertyChanged;
		}

		private void Layer_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "CopyrightText" || e.PropertyName == "Visibility")
				UpdateAttributionItems();
		}


		private void Layers_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			var oldItems = e.OldItems;
			System.Collections.IEnumerable newItems = e.NewItems;
			if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
				newItems = Map.Layers;
			if (oldItems != null)
				foreach (var item in oldItems)
					DetachLayerHandler(item as Layer);
			if (newItems != null)
				foreach (var item in newItems)
					AttachLayerHandler(item as Layer);
			UpdateAttributionItems();
		}

		#endregion

		#region Private Methods

		private void UpdateAttributionItems()
		{
			if (Map == null || Map.Layers == null)
				Items = null;
			else
			{
				var visibleCopyrights = Map.Layers.Where(layer => layer.Visibility == Visibility.Visible).OfType<ICopyright>();
				Items = visibleCopyrights.Select(cpr => cpr.CopyrightText).Where(cpr => !string.IsNullOrEmpty(cpr))
					.Select(cpr => cpr.Trim()).Distinct().ToList();
			}
		}

		#endregion
	}
}
