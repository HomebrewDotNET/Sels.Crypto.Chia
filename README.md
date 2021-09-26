# Sels.Crypto.Chia
This repository is meant to house current/future projects that are related to farming the crypto Chia.

## Projects for the Chia Blockchain (XCH)
These are my current projects for helping with plotting and farming on the Chia Blockchain

### Plot Bot
Plot Bot is a Linux systemd service for automating the plotting of drives managed from a single json configuration file. <br/>
The service is capable of automatically executing user defined plotting commands (using shell/bash) so it doesn't rely on a specific plotter like MadMax or the official one from the Chia dev team.

**Extended testing using Ubuntu 20.04.3 on my personal plotting vm. <br />
Should work with any Debian based distro. For other distro's I need some testers to confirm it works.** <br />

**Could work on Windows using WSL2. Need some testers to confirm and help me write instructions. <br />
I personally developed and tested the code using WSL2 on my Windows machine**

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
Instructions on how to install and configure Plot Bot can be found [here](https://github.com/Jenssels1998/Sels.Crypto.Chia/wiki/Getting-Started#plot-bot-service).

## Discord
Like my projects or just need some help? Join the Discord below for the best way to get in contact with me: <br/>
https://discord.gg/K4a6pwQAyY

## Donations
If you like what I do and want to support me on my future projects, feel free donate me some crypto by sending them to one of the below wallet addresses. <br/>

ATOM - cosmos1danhtnw9al7vjrwve5n7ayczjhrzt4sr37wjqw<br/>
ETH - 0x2aC6710b55e6426C09B8dcC2001B1776eB93C5A1<br/>
ADA - addr1q87d8msgu2kn3vps2dkr8egtt8m8jnm0xjyj2f7rued69w9zd79spv6m0pch7kqdyxcsr6fd372j2dhk98q20gj5sdasdrpug6<br/>
XCH - xch1pnzmvf3sdqh56u66q4grrp2jgt7qsphc25cc6z2p8cdvzq3l93jq6h4uwd<br/>
