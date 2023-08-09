var recognition;
var isListeningForAwakeKeyword = true;

var resumeKeyword = "keep going";
var cancelKeyword = "stop";
var eraseKeyword = "scratch";
var sendKeyword = "go";

const bot = window.speechSynthesis;

function startRecognition(resultCallback) {
    recognition = new window.webkitSpeechRecognition() || new window.SpeechRecognition();
    recognition.interimResults = true;
    recognition.lang = 'en-US'; // You can change the language here
    recognition.continuous = true;

    recognition.onresult = function (event) {
        var result = event.results[event.results.length - 1];

        if (result.isFinal) {

            if (bot.speaking) {
                bot.pause();
            }

            var transcript = result[0].transcript;

            if (transcript.toLowerCase().includes(resumeKeyword)) {
                bot.resume();
            } else if (transcript.toLowerCase().includes(cancelKeyword)) {
                bot.cancel();
            } else if (transcript.toLowerCase().includes(eraseKeyword)) {
                resultCallback.invokeMethodAsync('EraseBuffer');
            } else if (transcript.toLowerCase().includes(sendKeyword)) {
                resultCallback.invokeMethodAsync('SendBuffer');
            } else {
                resultCallback.invokeMethodAsync('AppendBuffer', transcript);
            }
        }
    };

    recognition.start();
}

function stopRecognition() {
    if (recognition) {
        recognition.stop();
    }
}


function speakText(text)  {
    var msg = new SpeechSynthesisUtterance(text);
    window.speechSynthesis.speak(msg);
}
