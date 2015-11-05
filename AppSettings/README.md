# Mash.AppSettings

Tired of littering your code with ConfigurationManager.AppSettings["TheSetting"] and parsing the string the type you want? We'll make loading settings easy with very little code investment.

Just create a data class which holds properties representing the settings you wish to load, and then call Load().
Mash.AppSettings use reflection on your data class to find public properties with our attribute and find a setting with that key in your app.config, and then set the value.

Now your unit tests won't have problems if they call code which directly loaded from the app.config. Instead they can set your settings class with any values they want.
This makes your code a lot more cohesive, and prevent unnecessary coupling to System.Configuration.

Don't want to store settings in your app.config file? No problem!
The loader uses an interface to determine where it gets its settings from so you can replace it later with another implementation.
This will prevent code changes of how you load settings from impacting the rest of your code base.

Your code will look like this:

<pre><code>var settings = new Settings();
AppSettingsLoader.Load(
    Factory.GetAppConfigSettingLoader(),
    ref settings);</code></pre>

Your settings file will look like this:
<pre><code>class MySettings
{
    [AppSetting]
    public int MyIntValue { get; set; }

    [AppSetting(Key = "StringSettingOverride")]
    public string OverridenSetting { get; set; }
}</code></pre>

## App.Config
Included is support for loading settings from your app.config file.

## Developer support
Useful information will be traced during loading. Watch your output window for any issues encountered.
Any problems loading values will be returned in an aggregate exception.