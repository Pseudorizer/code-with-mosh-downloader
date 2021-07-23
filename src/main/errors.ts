export class RethrownError extends Error {
  original_error: Error;
  stack_before_rethrow: string;

  constructor(message: string, error: Error){
	super(message);
	this.name = this.constructor.name;
	if (!error) throw new Error('RethrownError requires a message and error');
	this.original_error = error;
	this.stack_before_rethrow = this.stack;
	const message_lines =  (this.message.match(/\n/g)||[]).length + 1;
	this.stack = this.stack.split('\n').slice(0, message_lines+1).join('\n') + '\n' +
	  error.stack;
  }
}
