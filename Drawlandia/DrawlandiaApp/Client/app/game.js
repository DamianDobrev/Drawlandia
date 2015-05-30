//window.initDrawer = function () {
//    var draw = $.connection.drawHub;
//    $.connection.hub.start().done(function () {
        
//        draw.client.gameDraw = function (){
//            var context = document.getElementById('gameCanvas').getContext("2d");   

//            var paint = false;
//            var mousePosX;
//            var mousePosY;
//            var color = "#000000";

//            var clickX = new Array();
//            var clickY = new Array();
//            var clickDrag = new Array();
//            var colors = new Array();

//            function addClick(x, y, dragging, colorCur) {
//                clickX.push(x);
//                clickY.push(y);
//                clickDrag.push(dragging);
//                colors.push(colorCur);
//            }

//            function redraw() {
//                context.clearRect(0, 0, context.canvas.width, context.canvas.height);

//                context.lineJoin = "round";
//                context.lineWidth = 5;

//                for (var i = 0; i < clickX.length; i++) {
//                    context.strokeStyle = colors[i];
//                    context.beginPath();
//                    if (clickDrag[i] && i) {
//                        context.moveTo(clickX[i - 1], clickY[i - 1]);
//                    } else {
//                        context.moveTo(clickX[i] - 1, clickY[i]);
//                    }
//                    context.lineTo(clickX[i], clickY[i]);
//                    context.closePath();
//                    context.stroke();
//                }
//            }

//            //mouse interaction functions

//            $('#gameCanvas').mousedown(function (e) {
//                var canvasPos = $('#gameCanvas').offset();
//                mousePosX = e.pageX - this.offsetLeft - Math.round(canvasPos.left);
//                mousePosY = e.pageY - this.offsetTop - Math.round(canvasPos.top);

//                paint = true;
//                draw.server.draw(mousePosX, mousePosY, false, color);
//            });
//            $('#gameCanvas').mousemove(function (e) {
//                var canvasPos = $('#gameCanvas').offset();
//                mousePosX = e.pageX - this.offsetLeft - Math.round(canvasPos.left);
//                mousePosY = e.pageY - this.offsetTop - Math.round(canvasPos.top);

//                if (paint) {
//                    draw.server.draw(mousePosX, mousePosY, true, color);
//                }
//            });
//            $('#gameCanvas').mouseup(function (e) {
//                paint = false;
//            });
//            $('#gameCanvas').mouseleave(function (e) {
//                paint = false;
//            });

//            //instruments

//            $('#brushColorBlack').click(function (e) {
//                color = '#000000';
//            });
//            $('#brushColorRed').click(function (e) {
//                color = '#ff0000';
//            });
//            $('#brushEraser').click(function (e) {
//                color = '#ffffff';
//            });
//            $('#clearCanvas').click(function (e) {
//                draw.server.clear();
//            });

//            //functions called by server

//            draw.client.drawRemote = function (xRemote, yRemote, dragRemote, colorCurRemote) {
//                addClick(xRemote, yRemote, dragRemote, colorCurRemote);
//                redraw();
//            };

//            draw.client.clearCanvas = function () {
//                context.clearRect(0, 0, context.canvas.width, context.canvas.height);
//                clickX = new Array();
//                clickY = new Array();
//                clickDrag = new Array();
//                colors = new Array();
//                alert("CLEAR CANVAS CALLED FROM SERVER");
//            }
//        }
//    });
//};