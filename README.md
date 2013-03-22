# arcgis-toolkit-winstore

This project contains the set of controls for use with the Esri ArcGIS Runtime SDK for Windows Store apps.
Included are a number of controls you can use to enhance your applications. 


##What is in it?

ArcGIS Toolkit for Windows Store apps includes:

* `Legend` -  Displays a legend for a set of layers in your map.
* `Attribution` : Displays copyright/attribution information for layers in your map.

## How Do I Use It?

Register the xmlns namespace:
```xml
  xmlns:esriTK="using:ESRI.ArcGIS.Runtime.Toolkit.Xaml" 
```

<i>Note: Some of the following examples assumes an ArcGIS map control named 'MyMap' is present.</i>

####Legend
```xml
  <esriTK:Legend Layers="{Binding Layers, ElementName=MyMap}" 
    Scale="{Binding Scale, ElementName=MyMap}" />
```

####Attribution
```xml
  <esriTK:Attribution Layers="{Binding Layers, ElementName=MyMap}" />
```

## Resources

* [ArcGIS API for Windows Store apps](http://developers.arcgis.com/windows-store/)

## Issues

Find a bug or want to request a new feature?  Please let us know by submitting an issue.

## Contributing

Anyone and everyone is welcome to contribute. 

## Licensing
Copyright 2013 Esri

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

   http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

A copy of the license is available in the repository's [license.txt]( https://raw.github.com/Esri/arcgis-toolkit-winstore/master/license.txt) file.

[](Esri Tags: ArcGIS API WinStore WinRT)
[](Esri Language: C#)


