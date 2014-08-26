nopcommerce-plugin-windows-integrated
=====================================

nopcommerce windows integrated authentication plugin

-> maps windows accounts to nopcommerce accounts and creates them on demand
-> maps windows groups to internal nopcommerce groups and creates them on demand
-> mappings can be configured in plugin properties, there is no configuration page

installation
============

1. verify that you have a running nopcommerce
2. compile the plugin and install it
3. enable windows authentication for nopcommerce iis application
4. disable all other authentication methods
5. modify nopcommerce web.config to always authenticate with wia

add the following lines to your web.config

```xml
<authentication mode="Windows">
</authentication>
	
<authorization>
  <deny users="?"/>
</authorization>
```

notes
=====

this is more like a proof of concept. please review the code before using it for production environments
