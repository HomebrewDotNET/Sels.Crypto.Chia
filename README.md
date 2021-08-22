# Sels.Crypto.Chia
This repository is meant to house current/future projects that are related to farming the crypto Chia.

## Projects for the Chia Blockchain (XCH)
These are my current projects for helping with plotting and farming on the Chia Blockchain

### Plot Bot
Plot Bot is a Linux systemd service for automating the plotting of drives managed from a single json configuration file.
The service is capable of automatically executing user defined plotting commands (using shell/bash) so it doesn't rely on a specific plotter like MadMax or the official one from the Chia dev team.

#### Features
* Single json configuration file that can be modified without having to restart the service so no plotting effort is lost
* Dynamic plotting commands that make use of parameters that Plot Bot will fill out so maximum efficiency can be achieved without sacrificing customizability. (Works with official plotter, MadMax plotter, custom bash scripts, ...)
* Custom user defined plot sizes (K32, K33) so no service update is needed when we have to update the plot size
* Dynamic components for customizing the behaviour of Plot Bot
	* Plot delay components that tell Plot Bot when to start a new instance for a Plotter for maximum efficiency (Wait 30 minutes before starting a new instance, wait for previous instance to finish Phase 1, ...)
	* Drive clear components for clearing disk space when a drive is full (Clear Og plots to make room for Nft plots, ...)
* Support for multiple plotters with their own settings (A plotter for each plotting drive, plotters using different plot commands, ...)
* Plotters automatically divides resources among it's instances so no manual calculations are needed
* Automatically delete Og plots to make room for Nft plots so both Og plots and Nft plots can be farmed without losing too much revenue
* Automatic cleanup of unfinished plotting files when service has to stop so no manuel deletion is needed
* Log/Progress files that can be easily put on a network share so monitoring your plotting becomes easier from your workstation
* Mail logging so mails can be received from Plot Bot when it encounters issues
* Test mode so Plot Bot can be run without actually plotting to validate the configuration

#### Getting started
Instructions on how to install and configure Plot Bot can be found here: TODO Wiki