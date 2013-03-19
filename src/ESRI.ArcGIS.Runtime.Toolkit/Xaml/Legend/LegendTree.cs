// (c) Copyright ESRI.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using ESRI.ArcGIS.Runtime.Toolkit.Xaml.Primitives;
using ESRI.ArcGIS.Runtime.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Windows.UI.Xaml;

namespace ESRI.ArcGIS.Runtime.Toolkit.Xaml
{
	/// <summary>
	/// Internal class encapsulating a layer item representing the virtual root item for the legend tree.
	/// The LayerItems collection of this item is the collection of map layer item displayed at the first level of the TOC.
	/// This class manages the events coming from the map, from the map layers and from the map layer items.
	/// </summary>
	internal sealed class LegendTree : LayerItemViewModel
	{
		#region Constructor
		public LegendTree()
		{
			LayerItemsOptions = new LayerItemsOpts(returnMapLayerItems: false, returnGroupLayerItems: false,
			                                       returnLegendItems: false,
			                                       showOnlyVisibleLayers: true, reverseLayersOrder: false);
			Attach(this);
		}
		~LegendTree()
		{
			Detach();
		} 
		#endregion

		#region LegendItemTemplate
		/// <summary>
		/// Gets or sets the legend item template.
		/// </summary>
		/// <value>The legend item template.</value>
		private DataTemplate _legendItemTemplate;
		internal DataTemplate LegendItemTemplate
		{
			get
			{
				return _legendItemTemplate;
			}
			set
			{
 				if (_legendItemTemplate != value)
				{
					_legendItemTemplate = value;
					PropagateTemplate();
					UpdateLayerItemsOptions();
				}
			}
		}
		#endregion

		#region LayerTemplate
		private DataTemplate _layerTemplate;
		/// <summary>
		/// Gets or sets the layer template i.e. the template used to display a layer in the legend.
		/// </summary>
		/// <value>The layer template.</value>
		internal DataTemplate LayerTemplate
		{
			get
			{
				return _layerTemplate;
			}
			set
			{
 				if (_layerTemplate != value)
				{
					_layerTemplate = value;
					PropagateTemplate();
				}
			}
		}
		#endregion

		#region MapLayerTemplate
		private DataTemplate _mapLayerTemplate;
		/// <summary>
		/// Gets or sets the map layer template.
		/// </summary>
		/// <value>The map layer template.</value>
		internal DataTemplate MapLayerTemplate
		{
			get
			{
				return _mapLayerTemplate;
			}
			set
			{
 				if (_mapLayerTemplate != value)
				{
					_mapLayerTemplate = value;
					PropagateTemplate();
				}
			}
		}
		#endregion

		#region Map
		private Map _map;
		/// <summary>
		/// Gets or sets the map that the legend control is buddied to.
		/// </summary>
		/// <value>The map.</value>
		internal Map Map
		{
			get
			{
				return _map;
			}
			set
			{
				if (_map != value)
				{
					if (_map != null)
					{
						_map.PropertyChanged -= Map_PropertyChanged;
						_map.ExtentChanged -= MapOnViewportChanged;
						if (_map.Layers != null)
						{
							if (_map.Layers is INotifyCollectionChanged)
								(_map.Layers as INotifyCollectionChanged).CollectionChanged -= Layers_CollectionChanged;
							foreach (var l in _map.Layers)
								l.PropertyChanged -= Layer_PropertyChanged;
						}
					}

					_map = value;

					if (_map != null)
					{
						_map.PropertyChanged += Map_PropertyChanged;
						_map.ExtentChanged += MapOnViewportChanged;
						if (_map.Layers != null)
						{
							if (_map.Layers is INotifyCollectionChanged)
								(_map.Layers as INotifyCollectionChanged).CollectionChanged += Layers_CollectionChanged;
							foreach (var l in _map.Layers)
								l.PropertyChanged += Layer_PropertyChanged;
						}
					}
					UpdateMapLayerItems();
				}
			}
		}

		#endregion

		#region ShowOnlyVisibleLayers
		private bool _showOnlyVisibleLayers = true;
		/// <summary>
		/// Gets or sets a value indicating whether only the visible layers are participating to the legend.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if only the visible layers are participating to the legend; otherwise, <c>false</c>.
		/// </value>
		internal bool ShowOnlyVisibleLayers
		{
			get
			{
				return _showOnlyVisibleLayers;
			}
			set
			{
				_showOnlyVisibleLayers = value;
				LayerItemsOpts mode = LayerItemsOptions;
				mode.ShowOnlyVisibleLayers = value;
				PropagateLayerItemsOptions(mode);
			}
		}
		#endregion

		#region ReverseLayersOrder
		private bool _reverseLayersOrder;
		/// <summary>
		/// Gets or sets a value indicating whether only the visible layers are participating to the legend.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if only the visible layers are participating to the legend; otherwise, <c>false</c>.
		/// </value>
		internal bool ReverseLayersOrder
		{
			get
			{
				return _reverseLayersOrder;
			}
			set
			{
				_reverseLayersOrder = value;
				LayerItemsOpts mode = LayerItemsOptions;
				mode.ReverseLayersOrder = value;
				PropagateLayerItemsOptions(mode);
			}
		}
		#endregion

		#region Refresh
		/// <summary>
		/// Refreshes the legend control.
		/// </summary>
		/// <remarks>Note : In most cases, the control is always up to date without calling the refresh method.</remarks>
		internal void Refresh()
		{
			// refresh all map layer items (due to group layers we have to go through the legend hierarchy
			LayerItems.Descendants(item => item.LayerItems).OfType<MapLayerItem>().ForEach(mapLayerItem => mapLayerItem.Refresh());
		}
		#endregion

		#region Event Refreshed
		/// <summary>
		/// Occurs when the legend is refreshed. 
		/// Give the opportunity for an application to add or remove legend items.
		/// </summary>
		internal event EventHandler<Legend.RefreshedEventArgs> Refreshed;

		internal void OnRefreshed(object sender, Legend.RefreshedEventArgs args)
		{
			EventHandler<Legend.RefreshedEventArgs> refreshed = Refreshed;

			if (refreshed != null)
			{
				refreshed(sender, args);
			}
		}
		#endregion

		#region Map Event Handlers

		private ThrottleTimer updateTimer;

		private void MapOnViewportChanged(object sender, EventArgs e)
		{
			var map = sender as Map;
			if (map != null && map.Extent != null)
			{
				//Update Layer Visibilities is expensive, so wait for the map to stop navigating so
				//map navigation performance doesn't suffer from it.
				if (updateTimer == null)
				{
					updateTimer = new ThrottleTimer(50) { Action = UpdateLayerVisibilities };
				}
				updateTimer.Invoke();
			}
		}

		private void Map_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			//var map = sender as Map;
			//if (map == null)
			//	return;

			//if (e.PropertyName == "TimeExtent")
			//{
			//	// May change some layer visibilities for layer managing TimeExtent
			//	UpdateLayerVisibilities();
			//}
		}

		private void Layers_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.OldItems != null)
			{
				foreach (var item in e.OldItems.OfType<Layer>())
				{
					item.PropertyChanged -= Layer_PropertyChanged;
				}
			}
			if (e.NewItems != null)
			{
				foreach (var item in e.NewItems.OfType<Layer>())
				{
					item.PropertyChanged += Layer_PropertyChanged;
				}
			}
			UpdateMapLayerItems();
		}

		void Layer_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "ShowLegend")
				UpdateMapLayerItems();
		}

		#endregion

		#region Propagate methods propagating a property to all legend items of the legend tree
		private void PropagateTemplate()
		{
			// set the template on all descendants including the legend items
			LayerItems.Descendants(item => item.LayerItems).ForEach(item =>
			{
				item.Template = item.GetTemplate();
				item.LegendItems.ForEach(legendItem => legendItem.Template = legendItem.GetTemplate());
			});
		}

		private void PropagateLayerItemsOptions(LayerItemsOpts layerItemsOptions)
		{

			if (!LayerItemsOptions.Equals(layerItemsOptions))
			{
				DeferLayerItemsSourceChanged = true;
				LayerItemsOptions = layerItemsOptions;
				// set value on all descendants
				LayerItems.Descendants(layerItem => layerItem.LayerItems).ForEach(layerItem => layerItem.LayerItemsOptions = layerItemsOptions);
				DeferLayerItemsSourceChanged = false;
			}
		} 
		#endregion

		#region Private Methods

		private void UpdateMapLayerItems()
		{
			var mapLayerItems = new ObservableCollection<LayerItemViewModel>();

			if (Map != null)
			{
				UpdateMapLayerItemsRecursive(mapLayerItems, Map.Layers);
			}
			LayerItems = mapLayerItems;
		}

		private void UpdateMapLayerItemsRecursive(ObservableCollection<LayerItemViewModel> mapLayerItems, IEnumerable<Layer> layers)
		{
			foreach (Layer layer in layers.Where(l => l.ShowLegend))
			{
				MapLayerItem mapLayerItem = FindMapLayerItem(layer);

				if (mapLayerItem == null) // else reuse existing map layer item to avoid query again the legend and to keep the current state (selected, expansed, ..)
				{
					// Create a new map layer item
					mapLayerItem = new MapLayerItem(layer) { LegendTree = this };
					mapLayerItem.Refresh();
				}

				mapLayerItems.Add(mapLayerItem);
				if(layer is GroupLayer)
					UpdateMapLayerItemsRecursive(mapLayerItems, (layer as GroupLayer).ChildLayers);
			}
		}

		private IEnumerable<MapLayerItem> MapLayerItems
		{
			get
			{
				if (LayerItems == null)
					return null;

				return LayerItems.OfType<MapLayerItem>();
			}
		}

		private MapLayerItem FindMapLayerItem(Layer layer)
		{
			return MapLayerItems == null ? null : MapLayerItems.FirstOrDefault(mapLayerItem => mapLayerItem.Layer == layer);
		}

		internal void UpdateLayerVisibilities()
		{
			LayerItems.ForEach(layerItem =>
				{
					layerItem.DeferLayerItemsSourceChanged = true;
					layerItem.UpdateLayerVisibilities(true, true, true);
					layerItem.DeferLayerItemsSourceChanged = false;
				}
			);
		}
		#endregion

		#region LayerItemsMode

		private Legend.Mode _layerItemsMode = Legend.Mode.Flat;
		internal Legend.Mode LayerItemsMode
		{
			get
			{
				return _layerItemsMode;
			}
			set
			{
				if (value != _layerItemsMode)
				{
					_layerItemsMode = value;
					UpdateLayerItemsOptions();
				}
			}
		}

		private void UpdateLayerItemsOptions()
		{
			LayerItemsOpts layerItemsOptions;
			bool returnsLegendItems = (LegendItemTemplate != null);

			switch (LayerItemsMode)
			{
				case Legend.Mode.Tree:
					layerItemsOptions = new LayerItemsOpts(true, true, returnsLegendItems, ShowOnlyVisibleLayers, ReverseLayersOrder);
					break;

				default:
					layerItemsOptions = new LayerItemsOpts(false, false, returnsLegendItems, ShowOnlyVisibleLayers, ReverseLayersOrder);
					break;
			}

			PropagateLayerItemsOptions(layerItemsOptions);
		}

		#endregion


		/// <summary>
		/// The throttle timer is useful for limiting the number of requests to a method if
		/// the method is repeatly called many times but you only want the method raised once.
		/// It delays raising the method until a set interval, and any previous calls to the
		/// actions in that interval will be cancelled.
		/// </summary>
		private class ThrottleTimer
		{
			DispatcherTimer throttleTimer;

			internal ThrottleTimer(int milliseconds) : this(milliseconds, null) { }

			/// <summary>
			/// Initializes a new instance of the <see cref="ThrottleTimer"/> class.
			/// </summary>
			/// <param name="milliseconds">Milliseconds to throttle.</param>
			/// <param name="handler">The delegate to invoke.</param>
			internal ThrottleTimer(int milliseconds, Action handler)
			{
				this.Action = handler;
				throttleTimer = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(milliseconds) };
				throttleTimer.Tick += (s, e) =>
				{
					throttleTimer.Stop();
					if (this.Action != null)
						this.Action.Invoke();
				};
			}

			/// <summary>
			/// Delegate to Invoke.
			/// </summary>
			/// <value>The action.</value>
			public Action Action { get; set; }

			/// <summary>
			/// Invokes this instance (note that this will happen asynchronously and delayed).
			/// </summary>
			public void Invoke()
			{
				throttleTimer.Stop();
				throttleTimer.Start();
			}

			/// <summary>
			/// Cancels this timer if running.
			/// </summary>
			internal void Cancel()
			{
				throttleTimer.Stop();
			}
		}
	}
}
