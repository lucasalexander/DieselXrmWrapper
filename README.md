###Project Overview

The Diesel Xrm Service Wrapper provides a generic wrapper for the Dynamics CRM Organization Service that lets you create custom interfaces without custom code. The idea is that a power user or non-developer admin can create web service interfaces for external sytems to access Dynamics CRM without having to worry about authentication, authorization, WCF configuration, etc.

The first release of this project was made available as part of a post on the Alexander Development blog - "[Introducing the Diesel Xrm Service Wrapper](http://www.alexanderdevelopment.net/post/2013/07/31/introducing-the-diesel-xrm-service-wrapper)."

The solution is a .Net 4.5 WCF Service Application that can be hosted in IIS/WAS.

###Getting started

Configuration instructions can be found in on the [Getting Started](https://github.com/lucasalexander/DieselXrmWrapper/wiki/Getting-Started) page.

###Roadmap

The following features are currently planned:

1. Storing query FetchXml and related configuration data as custom entities in Dynamics CRM
2. Role-based authorization for individual queries
3. Varying impersonation settings per individual query
4. Updates
5. REST/JSON interfaces