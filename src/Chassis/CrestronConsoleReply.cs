using System;
using System.Text.RegularExpressions;

namespace PepperDash.Essentials.DM.Chassis
{
	/// <summary>
	/// Reads the identity a Crestron device's console reports.
	/// </summary>
	/// <remarks>
	/// <c>ver</c> answers with one line in a fixed shape -
	/// <c>HD-MD4X1-4K-ZE Cntrl Eng [v1.0083.0020 (Mar 13 2023), #8A1B2C3D] @E-00107fe12345</c> - the
	/// firmware between "[v" and " (", and the Ethernet MAC address after "@E-". The number after "#"
	/// is the device's TSID, not its serial number, so it is not reported as one. <c>host</c> answers
	/// <c>Host Name: NAME</c>.
	///
	/// Free of Crestron types so it can be tested on its own.
	/// </remarks>
	public static class CrestronConsoleReply
	{
		private static readonly Regex VerLine = new Regex(
			@"\[v(?<version>\d[\d.]*)\s*\((?<date>[^)]*)\)[^\]]*\](?:\s*@E-(?<mac>[0-9A-Fa-f]{12}))?",
			RegexOptions.Compiled);

		private static readonly Regex HostLine = new Regex(
			@"^\s*Host\s*Name\s*:\s*(?<host>\S+)\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		/// <summary>
		/// The firmware version, build date and MAC address from a <c>ver</c> reply; each null when
		/// the reply does not carry it.
		/// </summary>
		public static VerReply ParseVer(string text)
		{
			if (string.IsNullOrEmpty(text))
			{
				return new VerReply(null, null, null);
			}

			var match = VerLine.Match(text);
			if (!match.Success)
			{
				return new VerReply(null, null, null);
			}

			var mac = match.Groups["mac"].Success ? FormatMac(match.Groups["mac"].Value) : null;
			return new VerReply(match.Groups["version"].Value, match.Groups["date"].Value.Trim(), mac);
		}

		/// <summary>The host name from a <c>host</c> reply, or null.</summary>
		public static string ParseHostName(string text)
		{
			if (string.IsNullOrEmpty(text))
			{
				return null;
			}

			var match = HostLine.Match(text);
			return match.Success ? match.Groups["host"].Value : null;
		}

		private static readonly Regex Ipv4 = new Regex(@"\b(?:\d{1,3}\.){3}\d{1,3}\b", RegexOptions.Compiled);

		/// <summary>
		/// The address a device on IP ID <paramref name="ipid"/> is connected from, from the processor's own
		/// <c>ipt -t</c> (IP table) reply: the row whose first column is that IP ID (in hex, as the table lists it)
		/// and whose status is ONLINE, and the IPv4 address on it. A device that connects to the processor (peer)
		/// has no address in its IP table entry, so the device's ConnectedIpList is empty; the table still shows
		/// where it connected from. Null when no online row for the IP ID carries an address.
		/// </summary>
		public static string ParseIpTableAddress(string text, uint ipid)
		{
			if (string.IsNullOrEmpty(text)) return null;

			foreach (var raw in text.Split('\n'))
			{
				var line = raw.Trim();
				if (line.IndexOf("ONLINE", StringComparison.OrdinalIgnoreCase) < 0) continue;

				var first = line.Split(new[] { '|', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
				if (first.Length == 0) continue;
				uint id;
				var token = first[0].StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? first[0].Substring(2) : first[0];
				if (!uint.TryParse(token, System.Globalization.NumberStyles.HexNumber, null, out id) || id != ipid) continue;

				foreach (Match m in Ipv4.Matches(line))
				{
					var ip = m.Value;
					if (ip == "0.0.0.0" || ip.StartsWith("127.", StringComparison.Ordinal)) continue;
					var octets = ip.Split('.');
					var valid = true;
					foreach (var o in octets)
					{
						int n;
						if (!int.TryParse(o, out n) || n > 255) { valid = false; break; }
					}

					if (valid) return ip;
				}
			}

			return null;
		}

		/// <summary>Twelve hex digits as a colon-separated MAC address, lower case.</summary>
		public static string FormatMac(string hex)
		{
			if (hex == null || hex.Length != 12)
			{
				return null;
			}

			var lower = hex.ToLowerInvariant();
			return string.Join(":", new[]
			{
				lower.Substring(0, 2), lower.Substring(2, 2), lower.Substring(4, 2),
				lower.Substring(6, 2), lower.Substring(8, 2), lower.Substring(10, 2),
			});
		}
	}

	/// <summary>What a <c>ver</c> reply says about the device.</summary>
	public sealed class VerReply
	{
		public VerReply(string version, string date, string macAddress)
		{
			Version = version;
			Date = date;
			MacAddress = macAddress;
		}

		public string Version { get; private set; }

		public string Date { get; private set; }

		public string MacAddress { get; private set; }
	}
}
