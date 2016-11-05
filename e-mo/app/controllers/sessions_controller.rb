class SessionsController < ApplicationController
  skip_before_action :authenticate_user!, only: [:new, :create]

  def new
  end

  def create
    # 入力されたログインIDを持つユーザをDBから取得
    user = User.find_by(login_id: params[:session][:login_id])
    # ユーザが取得でき(存在するユーザだった)、かつ生徒だった場合
    if user && !user.teacher_flag
      # sessionにログイン対象のユーザ情報を保存
      log_in user
      # 生徒用ページにリダイレクト
      redirect_to student_url
      # ユーザが取得でき(存在するユーザだった)、かつ先生、かつパスワードが正しい場合
    elsif user && user.teacher_flag && user.password == (params[:session][:password])
      # sessionにログイン対象のユーザ情報を保存
      log_in user
      # 先生用ページにリダイレクト
      redirect_to teacher_url
    else
      # ログイン処理が行われなかった場合
      flash.now[:danger] = "ログインIDまたはパスワードが間違っています"
      render "new"
    end
  end
end
