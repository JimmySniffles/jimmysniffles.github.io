(function($) {

	var	$window = $(window),
		$body = $('body');

	// Breakpoints.
		breakpoints({
			wide:      [ '1281px',  '1680px' ],
			normal:    [ '981px',   '1280px' ],
			narrow:    [ '841px',   '980px'  ],
			narrower:  [ '737px',   '840px'  ],
			mobile:    [ '481px',   '736px'  ],
			mobilep:   [ null,      '480px'  ]
		});

	// Play initial animations on page load.
		$window.on('load', function() {
			window.setTimeout(function() {
				$body.removeClass('is-preload');
			}, 100);
		});
})(jQuery);

// Slide show scroll.
var slideIndex = 1;
showDivs1(slideIndex);
showDivs2(slideIndex);

function plusDivs1(n) {
	showDivs1(slideIndex += n);
}
function plusDivs2(n) {
	showDivs2(slideIndex += n);
}
function currentDiv1(n) {
	showDivs1(slideIndex = n);
}
function currentDiv2(n) {
	showDivs2(slideIndex = n);
}

function showDivs1(n) {
	var i;
	var x = document.getElementsByClassName("imageSlides1");
	var dots = document.getElementsByClassName("demo1");
	if (n > x.length) { slideIndex = 1 }
	if (n < 1) { slideIndex = x.length }
	for (i = 0; i < x.length; i++) {
		x[i].style.display = "none";  
	}
	for (i = 0; i < dots.length; i++) {
		dots[i].className = dots[i].className.replace(" w3-black", "");
	}
	x[slideIndex-1].style.display = "block";  
	dots[slideIndex-1].className += " w3-black";
}

function showDivs2(n) {
	var i;
	var x = document.getElementsByClassName("imageSlides2");
	var dots = document.getElementsByClassName("demo2");
	if (n > x.length) { slideIndex = 1 }
	if (n < 1) { slideIndex = x.length }
	for (i = 0; i < x.length; i++) {
		x[i].style.display = "none";  
	}
	for (i = 0; i < dots.length; i++) {
		dots[i].className = dots[i].className.replace(" w3-black", "");
	}
	x[slideIndex-1].style.display = "block";  
	dots[slideIndex-1].className += " w3-black";
}