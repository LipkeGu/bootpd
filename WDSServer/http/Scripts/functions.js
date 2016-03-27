function LoadDocument(url, target, title, needs)
{
	var xhttp = new XMLHttpRequest();
	if (xhttp != null)
	{
		xhttp.onreadystatechange = function() 
		{
			document.title = "Please wait...";

			if (this.readyState == 4)
			{
				switch(this.status)
				{
				case 200:
					genFrame(this.responseText, title, target);
				break;
				case 800:
					genFrame(MsgBox('info', title, xhttp.statusText), title, target);
				break;
				default:
					genFrame(MsgBox('error', title, 'Es trat ein Fehler bei der Anfrage auf!'), title, target);
				}
			} 
		};

		xhttp.open('GET', url, true);
		xhttp.send();
	}	
	else
	{
		MsgBox('error', title, 'Ajax wird vom Browser nicht unterstützt!', target);
	}
}

function MsgBox(type, title, message)
{
	var x = "<div class=\"title\"> " + title + "</div>\n";
	x += "<div id=\"" + type + "\">" + message + "</div>\n";

	return x;
}

function genFrame(content, title, target)
{
	var doc = "<h2>" + title + "</h2>\n";
	doc += content;

	document.title = title;
	document.getElementsByTagName(target)[0].innerHTML = doc;
	
	$(document).ready(function()
	{
		$("h2").fadeIn();
		$(target).fadeIn(1000);
	});
}

function refresh(url, target, title, needs, interval)
{
	var refreshId = setInterval(function()
	{
		LoadDocument(url, target, title, needs);
	}, interval * 1000);
}
